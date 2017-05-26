#include "libwbfsExt.h"

//-10 : Debugging exception, the n_wbfs_sec_per_disc were not equal!
//-11 : wbfs_sec_sz was not equal!
int wbfs_driveToDriveCopy
	(
		wbfs_t *srcPartitionPtr,
		wbfs_t *targetPartitionPtr,
		wbfs_disc_t* discPtr,		//Src disc
		progress_callback_t progressCallback
		//partition_selector_t selector
	)
{
	//Target
	int i, discn;
	u32 tot=0, cur=0;
	//u32 wii_sec_per_wbfs_sect = 1 << (targetPartitionPtr->wbfs_sec_sz_s-targetPartitionPtr->wii_sec_sz_s);
	//wiidisc_t *d = 0;
	//u8 *used = 0;
	wbfs_disc_info_t *info = 0;
	//u8* targetCopy_buffer = 0;		/may not be necessary, ,   checking if wbfs_sec_sz is always the same
	//u8 *b;
	int disc_info_sz_lba;

	//Src
	//wbfs_t *srcPartitionPtr = discPtr->p;	//unnecessary, already passed in both.
	u8* copy_buffer = 0;
	int src_wbs_nlb=srcPartitionPtr->wbfs_sec_sz/srcPartitionPtr->hd_sec_sz;
	//modified
	int dst_wbs_nlb=targetPartitionPtr->wbfs_sec_sz/targetPartitionPtr->hd_sec_sz;
	if(src_wbs_nlb!=dst_wbs_nlb)
		return -1;
	//Src
	copy_buffer = wbfs_ioalloc(srcPartitionPtr->wbfs_sec_sz);		
	
	//Src
	if(!copy_buffer)
		return -2;//ERR("alloc memory");


	//Target
	//used = wbfs_malloc(targetPartitionPtr->n_wii_sec_per_disc);
	/*
	if (!used)
	{
		ERR("unable to alloc memory");
	}*/

	//Target (modified)
	/*if (!copy_1_1)
	{*/
		//d = wd_open_disc(read_src_wii_disc, callback_data);
		//if(!discPtr)
		//{
		//	ERR("unable to open wii disc");
		//}
		//wd_build_disc_usage(discPtr, selector, used);
		//wd_close_disc(d);
		//d = 0;
	//}

	//Target
	for (i = 0; i < targetPartitionPtr->max_disc; i++) // find a free slot.
	{
		if (targetPartitionPtr->head->disc_table[i] == 0)
		{
			break;
		}
	}
	if (i == targetPartitionPtr->max_disc)
	{
		if(copy_buffer)
			wbfs_iofree(copy_buffer);
		return -3;//ERR("no space left on device (table full)");
	}
	//Target
	targetPartitionPtr->head->disc_table[i] = 1;
	discn = i;
	load_freeblocks(targetPartitionPtr);
	
	//Target    ,  build disc info
	info = wbfs_ioalloc(targetPartitionPtr->disc_info_sz);
	//b = (u8 *)info;
	memcpy(info->disc_header_copy,discPtr->header->disc_header_copy, 0x100);	//Modified			may be unnecesssary
	//read_src_wii_disc(callback_data, 0, 0x100, info->disc_header_copy);	Original


	//Target		, may be unnecessary,   checking if wbfs_sec_sz is always the same
	/*targetCopy_buffer = wbfs_ioalloc(targetPartitionPtr->wbfs_sec_sz);
	if (!targetCopy_buffer)
	{
		ERR("alloc memory");
	}*/

	//Both (used Src)
	if (progressCallback)
	{
		// count total number to write for spinner
		for (i = 0; i < srcPartitionPtr->n_wbfs_sec_per_disc; i++)
		{
			u32 iwlba = wbfs_ntohs(discPtr->header->wlba_table[i]);
			if (iwlba)
			{
				tot++;
				progressCallback(0, tot);
			}
		}
	}
	//if(targetPartitionPtr->wbfs_sec_sz!=srcPartitionPtr->wbfs_sec_sz)
	//	return -11;
	//if(targetPartitionPtr->n_wbfs_sec_per_disc!=srcPartitionPtr->n_wbfs_sec_per_disc)
	//	return -10;

	//Src
	for( i=0; i< srcPartitionPtr->n_wbfs_sec_per_disc; i++)
	{
		//Target
		u16 bl = 0;
		//Src
		u32 iwlba = wbfs_ntohs(discPtr->header->wlba_table[i]);
		//Target
		
		//Src
		if (iwlba)
		{
			bl = alloc_block(targetPartitionPtr);
			if (bl == 0xffff)
			{
				if(info)
					wbfs_iofree(info);
				if(copy_buffer)
					wbfs_iofree(copy_buffer);
				return -4;//ERR("no space left on device (disc full)");
			}
			srcPartitionPtr->read_hdsector(srcPartitionPtr->callback_data, srcPartitionPtr->part_lba + iwlba*src_wbs_nlb, src_wbs_nlb, copy_buffer);
			//write_dst_wii_sector(callback_data, i*dst_wbs_nlb, dst_wbs_nlb, copy_buffer);

			//Target
			// fix the partition table.
			/*if (i == (0x40000 >> targetPartitionPtr->wbfs_sec_sz_s))
			{
				wd_fix_partition_table(discPtr, selector, copy_buffer + (0x40000 & (targetPartitionPtr->wbfs_sec_sz - 1)));
			}*/
			//targetPartitionPtr->write_hdsector(targetPartitionPtr->callback_data, targetPartitionPtr->part_lba + bl * (dst_wbs_nlb), dst_wbs_nlb, copy_buffer);
			//targetPartitionPtr->write_hdsector(targetPartitionPtr->callback_data, srcPartitionPtr->part_lba + bl * (src_wbs_nlb), src_wbs_nlb, copy_buffer);
			targetPartitionPtr->write_hdsector(targetPartitionPtr->callback_data, targetPartitionPtr->part_lba + bl * dst_wbs_nlb, dst_wbs_nlb, copy_buffer);

			//Src
			cur++;
			if(progressCallback)
			{
				progressCallback(cur,tot);
				
				//Not correct when doing drive-to-drive
				//if(cur==tot)
				//{
				//	break;
				//}
			}

		}
		//Target
		info->wlba_table[i] = wbfs_htons(bl);
	}
	//Target, write disc info
	disc_info_sz_lba = targetPartitionPtr->disc_info_sz>>targetPartitionPtr->hd_sec_sz_s;
	targetPartitionPtr->write_hdsector(targetPartitionPtr->callback_data, targetPartitionPtr->part_lba + 1 + discn * disc_info_sz_lba,disc_info_sz_lba, info);
	wbfs_sync(targetPartitionPtr);
	//Src
	//wbfs_iofree(copy_buffer);
	
error:
	/*if(d)
		wd_close_disc(d);*/
	//if(used)
	//	wbfs_free(used);
	if(info)
		wbfs_iofree(info);
	if(copy_buffer)
		wbfs_iofree(copy_buffer);
	return 0;
}