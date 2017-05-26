// WIIDisc.cpp: implementation of the CWIIDisc class.
//
//////////////////////////////////////////////////////////////////////

#include "stdafx.h"
//#include "WIIScrubber.h"
#include "WIIDisc.h"
//#include "ProgressBox.h"
//#include "ResizePartition.h"
//#include "BootMode.h"
#include "ssl.h"
#include <direct.h>
#include <fcntl.h>


#ifdef _DEBUG
#undef THIS_FILE
static char THIS_FILE[]=__FILE__;
#define new DEBUG_NEW
#endif

//////////////////////////////////////////////////////////////////////
// Construction/Destruction
//////////////////////////////////////////////////////////////////////
#include <stdio.h>
#include <string.h>
#include <ctype.h>
#include <sys/types.h>
#include <io.h>

#include "global.h"
//#include "WIIScrubberDlg.h"

/* Trucha signature */
u8 trucha_signature[256] = {
	0x57, 0x61, 0x4E, 0x69, 0x4E, 0x4B, 0x6F, 0x4B,
	0x4F, 0x57, 0x61, 0x53, 0x48, 0x65, 0x52, 0x65,
	0x21, 0x8A, 0xB5, 0xBC, 0x89, 0x00, 0x8E, 0x5C,
	0x2B, 0xB6, 0x3E, 0x4D, 0x0A, 0xD7, 0xD2, 0xC4,
	0x97, 0x36, 0x82, 0xDF, 0x57, 0x06, 0x37, 0x27,
	0x96, 0xF1, 0x40, 0xD6, 0xCD, 0x36, 0xE4, 0xEE,
	0xC0, 0x99, 0xAA, 0x49, 0x99, 0x38, 0xA5, 0xC5,
	0xEE, 0xE3, 0x12, 0xF8, 0xBB, 0xE4, 0xBC, 0x52,
	0x1A, 0x3F, 0x31, 0x71, 0x45, 0x68, 0x98, 0xDB,
	0x5A, 0xD9, 0xB2, 0x27, 0x0F, 0x96, 0x15, 0xCF,
	0x2F, 0xBF, 0x18, 0xC8, 0xF7, 0xBD, 0x8D, 0xE5,
	0xA1, 0x9F, 0xDE, 0x5C, 0x83, 0x9A, 0xAE, 0x9D,
	0xD9, 0xDF, 0x0F, 0x1E, 0x47, 0xA7, 0xFA, 0xA1,
	0x80, 0xAC, 0xC8, 0x8F, 0x42, 0xDD, 0x5E, 0x71,
	0x9C, 0x76, 0x39, 0x93, 0x34, 0xC7, 0x79, 0xD5,
	0x66, 0x57, 0x31, 0xEA, 0xF1, 0xDF, 0x87, 0xCB,
	0xBE, 0x96, 0xE9, 0x05, 0x3E, 0xE3, 0xA7, 0xBE,
	0x8F, 0x6F, 0x4E, 0xD1, 0x4D, 0xAC, 0x42, 0xE9,
	0x23, 0x7C, 0x7D, 0x57, 0x43, 0xF6, 0x2C, 0xA9,
	0x4D, 0x5D, 0x93, 0x3E, 0x3C, 0x1B, 0x09, 0xFA,
	0xB1, 0xF3, 0xFF, 0xEF, 0xD6, 0xA6, 0xAE, 0x66,
	0x16, 0xFC, 0x37, 0x63, 0xA8, 0x7A, 0x4C, 0xCB,
	0xF6, 0xC9, 0x22, 0x39, 0xBF, 0x4E, 0xE2, 0x0C,
	0xAB, 0x76, 0x4B, 0xE7, 0x91, 0x54, 0xE1, 0x42,
	0x47, 0xE1, 0x32, 0x1E, 0x87, 0xE0, 0x84, 0x9D,
	0xDC, 0xBB, 0x00, 0x84, 0x35, 0x4D, 0x50, 0x2B,
	0x16, 0x72, 0x64, 0xD6, 0xC1, 0x47, 0xE2, 0x6C,
	0xBD, 0x2D, 0x54, 0x4E, 0x82, 0x35, 0x90, 0xC9,
	0x16, 0xC2, 0xE7, 0x9E, 0xA2, 0x6B, 0x3B, 0x7E,
	0x27, 0x3C, 0x03, 0x5C, 0x89, 0x53, 0x88, 0x9F,
	0xC5, 0xEC, 0x75, 0x86, 0x33, 0x58, 0xF3, 0xF0,
	0x85, 0x47, 0x3E, 0x07, 0x7C, 0xCF, 0xD1, 0x93
};

CWIIDisc::CWIIDisc()
{
	// create and blank the wii blank table
	pFreeTable = (unsigned char *) malloc(((u64)(4699979776) / (u64)(0x8000))*2);
	//set them all to clear first
	memset(pFreeTable, 0, ((u64)(4699979776) / (u64)(0x8000))*2);
	// then set the header size to used
	MarkAsUsed(0, 0x40000);

	pBlankSector = (unsigned char *) malloc(0x8000);
	memset(pBlankSector, 0xFF, 0x8000);

	pBlankSector0 = (unsigned char *) malloc(0x8000);
	memset (pBlankSector0, 0, 0x8000);


	//for (int i = 0; i < 20; i++)
	//{
	//	hPartition[i] = NULL;
	//}
	//hDisc = NULL;
	
	// then clear the decrypt key
	u8 key[16];

	memset(key,0,16);

	AES_KEY nKey;

	memset(&nKey, 0, sizeof(AES_KEY));
	AES_set_decrypt_key (key, 128, &nKey);

	//m_pParent = (CWIIScrubberDlg *) pParent;

}

CWIIDisc::~CWIIDisc()
{
	// free up the memory
	free(pFreeTable);
	free(pBlankSector);
	free(pBlankSector0);

}


u8 CWIIDisc::image_parse_header (struct part_header *header, u8 *buffer) {
        memset (header, 0, sizeof (struct part_header));

        header->console = buffer[0];
  
		// account for the Team Twizlers gotcha
		//if (FALSE==m_pParent->m_bFORCEWII)
		//{
			header->is_gc = (header->console == 'G') ||
				(header->console == 'D') ||
				(header->console == 'P') ||
				(header->console == 'U');
			header->is_wii = (header->console == 'R') ||
				(header->console == '_') ||
				(header->console == 'H') ||
				(header->console == '0') ||
				(header->console == '4');
		//}
		//else
		//{
		//	header->is_gc = FALSE;
		//	header->is_wii = TRUE;
		//}

        header->gamecode[0] = buffer[1];
        header->gamecode[1] = buffer[2];
        header->region = buffer[3];
        header->publisher[0] = buffer[4];
        header->publisher[1] = buffer[5];

        header->has_magic = be32 (&buffer[0x18]) == 0x5d1c9ea3;
        strncpy (header->name, (char *) (&buffer[0x20]), 0x60);

        header->dol_offset = be32 (&buffer[0x420]);

        header->fst_offset = be32 (&buffer[0x424]);
        header->fst_size = be32 (&buffer[0x428]);

        if (header->is_wii) {
                header->dol_offset *= 4;
                header->fst_offset *= 4;
                header->fst_size *= 4;
        }

        return header->is_gc || header->is_wii;
}

u32 CWIIDisc::parse_fst (u8 *fst, const char *names, u32 i, struct tree *tree,
               struct image_file *image, u32 part)
 {
        u64 offset;
        u32 size;
        const char *name;
        u32 j;

        name = names + (be32 (fst + 12 * i) & 0x00ffffff);
        size = be32 (fst + 12 * i + 8);

        if (i == 0)
		{
			// directory so need to go through the directory entries
                for (j = 1; j < size; )
				{
                        j = parse_fst (fst, names, j, tree, image, part);
				}
                return size;
        }

        if (fst[12 * i])
		{
				//m_csText += name;
				//m_csText += "\\";
    //            //AddToLog(m_csText);
				////pParent = m_pParent->AddItemToTree(name, pParent);

                for (j = i + 1; j < size; )
                        j = parse_fst (fst, names, j, NULL, image, part);

				// now remove the directory name we just added
				//m_csText = m_csText.Left(m_csText.GetLength()-strlen(name) - 1);
                return size;
        }
		else
		{
                offset = be32(fst + 12 * i + 4);
                if (image->parts[part].header.is_wii)
                        offset *= 4;

				//CString	csTemp;
				//csTemp.Format("%s [0x%lX] [0x%I64X] [0x%lX] [%d]",  name,
				/*											  part,
															  offset,
															  size,
															  i);*/

				//m_pParent->AddItemToTree(csTemp, pParent);


				//csTemp.Format("%s%s", m_csText, name);
                //AddToLog(csTemp, image->parts[part].offset + offset, size);
                

				MarkAsUsedDC(image->parts[part].offset+image->parts[part].data_offset, offset, size, image->parts[part].is_encrypted);

                image->nfiles++;
                image->nbytes += size;

                return i + 1;
        }
}
u32 CWIIDisc::parse_fst_and_save(u8 *fst, const char *names, u32 i,
								 struct image_file *image, u32 part)
 {
		//MSG msg;
        u64 offset;
        u32 size;
        const char *name;
        u32 j;
		//char*	csTemp;

        name = names + (be32 (fst + 12 * i) & 0x00ffffff);
        size = be32 (fst + 12 * i + 8);

		//pProgressBox->SetPosition(i);
   //     if (PeekMessage(&msg,
   //         NULL,
   //         0,
   //         0,
   //         PM_REMOVE))
   //     {
   //         // PeekMessage has found a message--process it 
   //         if (msg.message != WM_CANCELLED)
   //         {
   //             TranslateMessage(&msg); // Translate virt. key codes 
   //             DispatchMessage(&msg);  // Dispatch msg. to window 
   //         }
			//else
			//{
			//	// show a complete exit
			//	return 0xFFFFFFFF;
			//}
   //     }

        if (i == 0)
		{
			// directory so need to go through the directory entries
                for (j = 1; j < size; )
				{
                        j = parse_fst_and_save(fst, names, j, image, part);
				}
				if (j!=0xFFFFFFFF)
				{
					return size;
				}
				else
				{
					return 0xFFFFFFFF;
				}
        }

        if (fst[12 * i])
		{
			// directory so....
			// create a directory and change to it
			_mkdir(name);
			_chdir(name);
	
			
			for (j = i + 1; j < size; )
			{
				j = parse_fst_and_save(fst, names, j, image, part);
			}
			
			// now remove the directory name we just added
			//m_csText = m_csText.Left(m_csText.GetLength()-strlen(name) - 1);
			_chdir("..");
			if (j!=0xFFFFFFFF)
			{
				return size;
			}
			else
			{
				return 0xFFFFFFFF;
			}
        }
		else
		{
			// it's a file so......
			// create a filename and then save it out
			
			offset = be32(fst + 12 * i + 4);
			if (image->parts[part].header.is_wii)
			{
				offset *= 4;
			}

			// now save it
			if (TRUE==SaveDecryptedFile(name, image, part, offset, size))
			{
				return i + 1;
			}
			else
			{
				// Error writing file
				return 0xFFFFFFFF;
			}
        }
}

BOOL CWIIDisc::ExportFile(char* filename, char* targetPath, image_file *image, u32 nPartition, bool topLevelOnly)
{


	u8 * fst = (u8 *) (malloc ((u32)(image->parts[nPartition].header.fst_size)));

    if (io_read_part (fst, (u32)(image->parts[nPartition].header.fst_size),image, nPartition, image->parts[nPartition].header.fst_offset) !=
                            image->parts[nPartition].header.fst_size)
	{
           //AfxMessageBox("fst.bin");
		free (fst);
		return FALSE;
    }
	u32 nfiles = be32 (fst + 8);
	u32 nFiles = searchAndSaveFile(fst, (char *) (fst + 12 * nfiles), 0 , image, nPartition, filename, targetPath, topLevelOnly);
	
	free(fst);

	if(nFiles== 0xFFFFFFFF)
		return FALSE;
	//delete pProgressBox;
	if(nFiles==0xFFFFFFFF-1)
		return TRUE;
	return FALSE;
}

u32 CWIIDisc::searchAndSaveFile(u8 *fst, const char *names, u32 i, struct image_file *image, u32 part, char* fileToSearch, char* targetPath, bool topLevelOnly)
{
			//MSG msg;
        u64 offset;
        u32 size;
        const char *name;
        u32 j;
		//char*	csTemp;

        name = names + (be32 (fst + 12 * i) & 0x00ffffff);
        size = be32 (fst + 12 * i + 8);

		//pProgressBox->SetPosition(i);
   //     if (PeekMessage(&msg,
   //         NULL,
   //         0,
   //         0,
   //         PM_REMOVE))
   //     {
   //         // PeekMessage has found a message--process it 
   //         if (msg.message != WM_CANCELLED)
   //         {
   //             TranslateMessage(&msg); // Translate virt. key codes 
   //             DispatchMessage(&msg);  // Dispatch msg. to window 
   //         }
			//else
			//{
			//	// show a complete exit
			//	return 0xFFFFFFFF;
			//}
   //     }

        if (i == 0)
		{
			// directory so need to go through the directory entries
                for (j = 1; j < size; )
				{
                        j = searchAndSaveFile(fst, names, j, image, part, fileToSearch, targetPath, topLevelOnly);
				}
				if (j!=0xFFFFFFFF && j!=(0xFFFFFFFF-1))
				{
					return size;
				}
				else if(j==(0xFFFFFFFF-1))
				{
					return 0xFFFFFFFF-1;
				}
				else
				{
					return 0xFFFFFFFF;
				}
        }

        if (fst[12 * i] && !topLevelOnly)
		{
			// directory so....
			//// create a directory and change to it
			/*_mkdir(name);
			_chdir(name);*/
	
			
			for (j = i + 1; j < size; )
			{
				j = searchAndSaveFile(fst, names, j, image, part, fileToSearch, targetPath, topLevelOnly);
			}
			
			// now remove the directory name we just added
			//m_csText = m_csText.Left(m_csText.GetLength()-strlen(name) - 1);
			//_chdir("..");
			if (j!=0xFFFFFFFF)
			{
				return size;
			}
			else
			{
				return 0xFFFFFFFF;
			}
        }
		else
		{
			// it's a file so......
			// create a filename and then save it out
			
			offset = be32(fst + 12 * i + 4);
			if (image->parts[part].header.is_wii)
			{
				offset *= 4;
			}
			int lStrcmp = strcmp(name, fileToSearch);
			if(lStrcmp==0)
			{// now save it
				if (TRUE==SaveDecryptedFile(targetPath, image, part, offset, size))
				{
					return 0xFFFFFFFF-1;
				}
				else
				{
					// Error writing file
					return 0xFFFFFFFF;
				}
			}
        }
		return i+1;
}

u8 CWIIDisc::get_partitions (struct image_file *image) {
        u8 buffer[16];
        u64 part_tbl_offset;
        u64 chan_tbl_offset;
        u32 i;

        u8 title_key[16];
        u8 iv[16];
        u8 partition_key[16];

        u32 nchans;


		// clear out the old memory allocated
		if (NULL!=image->parts)
		{
			free (image->parts);
			image->parts = NULL;
		}
        io_read (buffer, 16, image, 0x40000);
        image->nparts = 1 + be32 (buffer);

        nchans = be32 (&buffer[8]);

		// number of partitions is out by one
        /*AddToLog("number of partitions:", image->nparts);
        AddToLog("number of channels:", nchans);*/

		// store the values for later bit twiddling
		image->ChannelCount = nchans;
		image->PartitionCount = image->nparts -1;

        image->nparts += nchans;

 
        part_tbl_offset = u64 (be32 (&buffer[4])) * ((u64)(4));
        chan_tbl_offset = (u64 )(be32 (&buffer[12])) * ((u64) (4));
        //AddToLog("partition table offset: ", part_tbl_offset);
        //AddToLog("channel table offset: ", chan_tbl_offset);

		image->part_tbl_offset = part_tbl_offset;
		image->chan_tbl_offset = chan_tbl_offset;

        image->parts = (struct partition *)
                        malloc (image->nparts * sizeof (struct partition));
        memset (image->parts, 0, image->nparts * sizeof (struct partition));

        for (i = 1; i < image->nparts; ++i) {
				//AddToLog("--------------------------------------------------------------------------");
                //AddToLog("partition:", i);

                if (i < image->nparts - nchans)
				{
                        io_read (buffer, 8, image,
                                 part_tbl_offset + (i - 1) * 8);

                        switch (be32 (&buffer[4]))
						{
                        case 0:
                                image->parts[i].type = PART_DATA;
                                break;

                        case 1:
                                image->parts[i].type = PART_UPDATE;
                                break;

                        case 2:
                                image->parts[i].type = PART_INSTALLER;
                                break;

                        default:
                                break;
                        }

                } else {
						//AddToLog("Virtual console");
                        
						// error in WiiFuse as it 'assumes' there are only two
						// partitions before VC games

						// changed to a generic version
						io_read (buffer, 8, image,
                                 chan_tbl_offset + (i - image->PartitionCount - 1) * 8);

                        image->parts[i].type = PART_VC;
                        image->parts[i].chan_id[0] = buffer[4];
                        image->parts[i].chan_id[1] = buffer[5];
                        image->parts[i].chan_id[2] = buffer[6];
                        image->parts[i].chan_id[3] = buffer[7];
                }

                image->parts[i].offset = (u64)(be32 (buffer)) * ((u64)(4));

                //AddToLog("partition offset: ", image->parts[i].offset);

				// mark the block as used
				MarkAsUsed(image->parts[i].offset, 0x8000);


                io_read (buffer, 8, image, image->parts[i].offset + 0x2b8);
                image->parts[i].data_offset = (u64)(be32 (buffer)) << 2;
                image->parts[i].data_size = (u64)(be32 (&buffer[4])) << 2;

				// now get the H3 offset
				io_read (buffer,4, image, image->parts[i].offset + 0x2b4);
				image->parts[i].h3_offset = (u64)(be32 (buffer)) << 2 ;

                /*AddToLog("partition data offset:", image->parts[i].data_offset);
                AddToLog("partition data size:", image->parts[i].data_size);
                AddToLog("H3 offset:", image->parts[i].h3_offset);*/

                tmd_load (image, i);
                if (image->parts[i].tmd == NULL) {
                        //AddToLog("partition has no valid tmd");

                        continue;
                }

               sprintf (image->parts[i].title_id_str, "%016llx",
                         image->parts[i].tmd->title_id);

                image->parts[i].is_encrypted = 1;
                image->parts[i].cached_block = 0xffffffff;

                memset (title_key, 0, 16);
                memset (iv, 0, 16);

                io_read (title_key, 16, image, image->parts[i].offset + 0x1bf);
                io_read (iv, 8, image, image->parts[i].offset + 0x1dc);


                AES_cbc_encrypt (title_key, partition_key, 16,
                                 &image->key, iv, AES_DECRYPT);
                
				memcpy(image->parts[i].title_key, partition_key, 16);

				AES_set_decrypt_key (partition_key, 128, &image->parts[i].key);

                sprintf (image->parts[i].key_c, "0x"
                         "%02x%02x%02x%02x%02x%02x%02x%02x"
                         "%02x%02x%02x%02x%02x%02x%02x%02x",
                          partition_key[0], partition_key[1],
                          partition_key[2], partition_key[3],
                          partition_key[4], partition_key[5],
                          partition_key[6], partition_key[7],
                          partition_key[8], partition_key[9],
                          partition_key[10], partition_key[11],
                          partition_key[12], partition_key[13],
                          partition_key[14], partition_key[15]);


        }

        return image->nparts == 0;
}

struct image_file * CWIIDisc::image_init (const char *filename)
{
        int fp;
        struct image_file *image;
        struct part_header *header;

        u8 buffer[0x440];

		//m_csFilename = "";

        fp = _open (filename, _O_BINARY|_O_RDWR);
        if (fp == -1) {
                //AfxMessageBox(filename);
                return NULL;
        }

		// get the filesize and set the range accordingly for future
		// operations
		
		nImageSize = _lseeki64(fp, 0L, SEEK_END);

        image = (struct image_file *) malloc (sizeof (struct image_file));

        if (!image) {
                // LOG_ERR ("out of memory");
                _close (fp);
                return NULL;
        }

        memset (image, 0, sizeof (struct image_file));
        image->fp = fp;

        if (!io_read (buffer, 0x440, image, 0)) {
                //AfxMessageBox("reading header");
                _close (image->fp);
                free (image);
                return NULL;
        }

        header = (struct part_header *) (malloc (sizeof (struct part_header)));

        if (!header) {
                _close (image->fp);
                free (image);
                // LOG_ERR ("out of memory");
                return NULL;
        }

        image_parse_header (header, buffer);

        if (!header->is_gc && !header->is_wii) {
                // LOG_ERR ("unknown type for file: %s", filename);
                _close (image->fp);
                free (header);
                free (image);
                return NULL;
        }

        if (!header->has_magic)
		{
                //AddToLog("image has an invalid magic");

		}

        image->is_wii = header->is_wii;

        if (header->is_wii) {

			if (FALSE==CheckAndLoadKey(TRUE, image))
			{
				free (image);
				return NULL;
			}
        }

		// Runtime crash fixed :)
		// Identified by Juster over on GBATemp.net
		// the free was occuring before in the wrong location
		// As a free was being carried out and the next line was checking
		// a value it was pointing to
        free (header);
        return image;
};

int CWIIDisc::image_parse (struct image_file *image) {
        u8 buffer[0x440];
        u8 *fst;
        u32 i;
        u8 j, valid, nvp;

        u32 nfiles;
		
		//HTREEITEM hPartitionBin;

		//CString csText;


        if (image->is_wii) {
                //AddToLog("wii image detected");
				//hDisc = m_pParent->AddItemToTree("WII DISC", NULL);

                get_partitions (image);
        } else {
                //AddToLog("gamecube image detected");

                image->parts = (struct partition *)
                                malloc (sizeof (struct partition));
                memset (&image->parts[0], 0, sizeof (struct partition));
                image->nparts = 1;
				image->PartitionCount = 1;
				image->ChannelCount = 0;
				image->part_tbl_offset = 0;
				image->chan_tbl_offset = 0;
                image->parts[0].type = PART_DATA;
				//hDisc = m_pParent->AddItemToTree("GAMECUBE DISC", NULL);
        }

        _fstat (image->fp, &image->st);


        nvp = 0;
        for (i = 0; i < image->nparts; ++i)
        {
				//AddToLog("------------------------------------------------------------------------------");

                //AddToLog("partition:", i);

				//switch (image->parts[i].type)
				//{
				//case PART_DATA:
				//	//csText.Format("Partition:%d - DATA",i);
    //                            break;

				//case PART_UPDATE:
				//	//csText.Format("Partition:%d - UPDATE",i);
    //                            break;

				//case PART_INSTALLER:
				//	//csText.Format("Partition:%d - INSTALLER",i);
    //                            break;
				//case PART_VC:
				//	//csText.Format("Partition:%d - VC GAME [%s]",i,image->parts[i].chan_id );
    //                            break;
				//default:
				//	if (0!=i)
				//	{
				//		//csText.Format("Partition:%d - UNKNOWN",i);
				//	}
				//	else
				//	{
				//		//csText.Format("Partition:0 - PART INFO");
				//	}
				//	break; 
				//}

				//hPartition[i] = m_pParent->AddItemToTree(csText, hDisc);

                if (!io_read_part (buffer, 0x440, image, i, 0)) {
                        //AfxMessageBox("partition header");
                        return 1;
                }

                valid = 1;
                for (j = 0; j < 6; ++j) {
                        if (!isprint (buffer[j])) {
                                valid = 0;
                                break;
                        }
                }

                if (!valid) {
                        //AddToLog("invalid header for partition:", i);
                        continue;
                }
                nvp++;


                image_parse_header (&image->parts[i].header, buffer);




				if (PART_UNKNOWN!=image->parts[i].type)
				{
					/*AddToLog("\\partition.bin", image->parts[i].offset, image->parts[i].data_offset);
					csText.Format("%s [0x%lX] [0x%I64X] [0x%I64X] [-4]",
						"partition.bin",
						i,
						image->parts[i].offset,
						image->parts[i].data_offset);*/
					MarkAsUsed(image->parts[i].offset, image->parts[i].data_offset);
					//hPartitionBin = m_pParent->AddItemToTree(csText, hPartition[i]);

					// add on the boot.bin
					//AddToLog("\\boot.bin", image->parts[i].offset + image->parts[i].data_offset, (u64)0x440);
					MarkAsUsedDC(image->parts[i].offset + image->parts[i].data_offset, 0, (u64)0x440, image->parts[i].is_encrypted);
					/*csText.Format("%s [0x%lX] [0x%I64X] [0x%I64X] [0]",
						"boot.bin",
						i,
						(u64) 0x0,
						(u64)0x440);*/
					
					//m_pParent->AddItemToTree(csText, hPartition[i]);
					
					
					// add on the bi2.bin
					//AddToLog("\\bi2.bin", image->parts[i].offset + image->parts[i].data_offset + 0x440, (u64)0x2000);
					MarkAsUsedDC(image->parts[i].offset + image->parts[i].data_offset, 0x440, (u64)0x2000, image->parts[i].is_encrypted);
					/*csText.Format("%s [0x%lX] [0x%I64X] [0x%I64X] [0]",
						"bi2.bin",
						i,
						(u64) 0x440,
						(u64)0x2000);*/
					
					//m_pParent->AddItemToTree(csText, hPartition[i]);
					
				}
                io_read_part (buffer, 8, image, i, 0x2440 + 0x14);
                image->parts[i].appldr_size =
                        be32 (buffer) + be32 (&buffer[4]);
                if (image->parts[i].appldr_size > 0)
                        image->parts[i].appldr_size += 32;


				if (image->parts[i].appldr_size > 0)
				{
					//AddToLog("\\apploader.img", image->parts[i].offset+ image->parts[i].data_offset +0x2440, image->parts[i].appldr_size);
					MarkAsUsedDC(	image->parts[i].offset + image->parts[i].data_offset,
									0x2440,
									image->parts[i].appldr_size,
									image->parts[i].is_encrypted);
					/*csText.Format("%s [0x%lX] [0x%I64X] [0x%I64X] [-3]",
										"apploader.img",
										i,
										(u64) 0x2440,
										(u64) image->parts[i].appldr_size);*/

					//m_pParent->AddItemToTree(csText, hPartition[i]);
				}
				else
				{
					//AddToLog("apploader.img not present");
				}

                if (image->parts[i].header.dol_offset > 0)
				{
                        io_read_part (buffer, 0x100, image, i, image->parts[i].header.dol_offset);
                        image->parts[i].header.dol_size = get_dol_size (buffer);

						// now check for error condition with bad main.dol
						if (image->parts[i].header.dol_size >=image->parts[i].data_size)
						{
							// almost certainly an error as it's bigger than the partition
							image->parts[i].header.dol_size = 0;
						}
						MarkAsUsedDC(	image->parts[i].offset+ image->parts[i].data_offset,
										image->parts[i].header.dol_offset,
										image->parts[i].header.dol_size,
										image->parts[i].is_encrypted);
						
						/*AddToLog("\\main.dol ", image->parts[i].offset + image->parts[i].data_offset + image->parts[i].header.dol_offset, image->parts[i].header.dol_size);

						csText.Format("%s [0x%lX] [0x%I64X] [0x%I64X] [-2]",
										"main.dol",
										i,
										image->parts[i].header.dol_offset,
										image->parts[i].header.dol_size);*/

						//m_pParent->AddItemToTree(csText, hPartition[i]);
                } else{

                        //AddToLog("partition has no main.dol");
				}

				if (image->parts[i].is_encrypted)
				{
					// Now add the TMD.BIN and cert.bin files - as these are part of partition.bin
					// we don't need to mark them as used - we do put them undr partition.bin in the
					// tree though

					/*AddToLog("\\tmd.bin", image->parts[i].offset + image->parts[i].tmd_offset, image->parts[i].tmd_size);
					csText.Format("%s [0x%lX] [0x%I64X] [0x%I64X] [-5]",
						"tmd.bin",
						i,
						image->parts[i].tmd_offset,
						image->parts[i].tmd_size);*/
					
					//m_pParent->AddItemToTree(csText, hPartitionBin);

					/*AddToLog("\\cert.bin", image->parts[i].offset + image->parts[i].cert_offset, image->parts[i].cert_size);
					csText.Format("%s [0x%lX] [0x%I64X] [0x%I64X] [-6]",
						"cert.bin",
						i,
						(u64)image->parts[i].cert_offset,
						(u64)image->parts[i].cert_size);*/
					
					//m_pParent->AddItemToTree(csText, hPartitionBin);


				
					// add on the H3
					//AddToLog("\\h3.bin", image->parts[i].offset + image->parts[i].h3_offset, (u64)0x18000);
					MarkAsUsedDC(	image->parts[i].offset,
									image->parts[i].h3_offset,
									(u64)0x18000,
									FALSE);

					/*csText.Format("%s [0x%lX] [0x%I64X] [0x%I64X] [-7]",
										"h3.bin",
										i,
										(u64) image->parts[i].h3_offset,
										(u64)0x18000);*/

					//m_pParent->AddItemToTree(csText, hPartitionBin);

				}
				
                
				if (image->parts[i].header.fst_offset > 0 &&
                    image->parts[i].header.fst_size > 0) {

                        //AddToLog("\\fst.bin ", image->parts[i].offset+image->parts[i].data_offset+image->parts[i].header.fst_offset,image->parts[i].header.fst_size);

						MarkAsUsedDC( image->parts[i].offset+ image->parts[i].data_offset,
									  image->parts[i].header.fst_offset,
									  image->parts[i].header.fst_size,
									  image->parts[i].is_encrypted);
						/*csText.Format("%s [0x%lX] [0x%I64X] [0x%I64X] [-1]",
										"fst.bin",
										i,
										image->parts[i].header.fst_offset,
										image->parts[i].header.fst_size);*/
						
						//m_pParent->AddItemToTree(csText, hPartition[i]);

                        fst = (u8 *) (malloc ((u32)(image->parts[i].header.fst_size)));
                        if (io_read_part (fst, (u32)(image->parts[i].header.fst_size),
                                          image, i,
                                          image->parts[i].header.fst_offset) !=
                            image->parts[i].header.fst_size)
						{
                                //AfxMessageBox("fst.bin");
								free (fst);
                                return 1;
                        }

                        nfiles = be32 (fst + 8);

                        if (12 * nfiles > image->parts[i].header.fst_size) {
                                //AddToLog("invalid fst for partition", i);
                        } else {
								//m_csText = "\\";

                                parse_fst (fst, (char *) (fst + 12 * nfiles), 0,
                                           NULL, image, i);
                        }

                        free (fst);
                } else
				{
                        //AddToLog("partition has no fst");
				}


        }

        if (!nvp) {
                //AddToLog("no valid partition were found, exiting");
                return 1;
        }

        return 0;
}

void CWIIDisc::image_deinit (struct image_file *image) {
        u32 i;

        if (image == NULL)
                return;


        if (image->parts) {
                for (i = 0; i < image->nparts; ++i)
                        if (image->parts[i].tmd)
                                tmd_free (image->parts[i].tmd);
                        
                free (image->parts);
        }

        _close (image->fp);


        free (image);
		//hDisc = NULL;
		/*for (int x=0;x<20;x++)
		{
			hPartition[x] = NULL;
		}*/
}

void CWIIDisc::tmd_load (struct image_file *image, u32 part) {
        struct tmd *tmd;
        u32 tmd_offset, tmd_size;
        enum tmd_sig sig = SIG_UNKNOWN;

        u64 off, cert_size, cert_off;
        u8 buffer[64];
        u16 i, s;

        off = image->parts[part].offset;
        io_read (buffer, 16, image, off + 0x2a4);

        tmd_size = be32 (buffer);
        tmd_offset = be32 (&buffer[4]) * 4;
        cert_size = be32 (&buffer[8]);
        cert_off = be32 (&buffer[12]) * 4;

        // TODO: ninty way?
        /*
        if (cert_size)
                image->parts[part].tmd_size =
                        cert_off - image->parts[part].tmd_offset + cert_size;
        */

        off += tmd_offset;

        io_read (buffer, 4, image, off);
        off += 4;

        switch (be32 (buffer)) {
        case 0x00010001:
                sig = SIG_RSA_2048;
                s = 0x100;
                break;

        case 0x00010000:
                sig = SIG_RSA_4096;
                s = 0x200;
                break;
        }

        if (sig == SIG_UNKNOWN)
                return;

        tmd = (struct tmd *) malloc (sizeof (struct tmd));
        memset (tmd, 0, sizeof (struct tmd));

        tmd->sig_type = sig;

        image->parts[part].tmd = tmd;
        image->parts[part].tmd_offset = tmd_offset;
        image->parts[part].tmd_size = tmd_size;

		image->parts[part].cert_offset = cert_off;
		image->parts[part].cert_size = cert_size;

        tmd->sig = (unsigned char *) malloc (s);
        io_read (tmd->sig, s, image, off);
        off += s;
        
        off = ROUNDUP64B(off);

        io_read ((unsigned char *)&tmd->issuer[0], 0x40, image, off);
        off += 0x40;

        io_read (buffer, 26, image, off);
        off += 26;

        tmd->version = buffer[0];
        tmd->ca_crl_version = buffer[1];
        tmd->signer_crl_version = buffer[2];

        tmd->sys_version = be64 (&buffer[4]);
        tmd->title_id = be64 (&buffer[12]);
        tmd->title_type = be32 (&buffer[20]);
        tmd->group_id = be16 (&buffer[24]);

        off += 62;

        io_read (buffer, 10, image, off);
        off += 10;

        tmd->access_rights = be32 (buffer);
        tmd->title_version = be16 (&buffer[4]);
        tmd->num_contents = be16 (&buffer[6]);
        tmd->boot_index = be16 (&buffer[8]);

        off += 2;

        if (tmd->num_contents < 1)
                return;

        tmd->contents =
                (struct tmd_content *)
                malloc (sizeof (struct tmd_content) * tmd->num_contents);

        for (i = 0; i < tmd->num_contents; ++i) {
                io_read (buffer, 0x30, image, off);
                off += 0x30;

                tmd->contents[i].cid = be32 (buffer);
                tmd->contents[i].index = be16 (&buffer[4]);
                tmd->contents[i].type = be16 (&buffer[6]);
                tmd->contents[i].size = be64 (&buffer[8]);
                memcpy (tmd->contents[i].hash, &buffer[16], 20);

        }

        return;
}

void CWIIDisc::tmd_free (struct tmd *tmd) {
        if (tmd == NULL)
                return;

        if (tmd->sig)
                free (tmd->sig);

        if (tmd->contents)
                free (tmd->contents);

        free (tmd);
}

int CWIIDisc::io_read (unsigned char  *ptr, size_t size, struct image_file *image, u64 offset) {
        size_t bytes;

		__int64 nSeek;

		nSeek = _lseeki64 (image->fp, offset, SEEK_SET);

        if (-1==nSeek) {
			//DWORD x = GetLastError();
                //AfxMessageBox("io_seek");
                return -1;
        }

		MarkAsUsed(offset, size);

        bytes = _read(image->fp, ptr, size);

        if (bytes != size)
                //AfxMessageBox("io_read");

        return bytes;
}

int CWIIDisc::decrypt_block (struct image_file *image, u32 part, u32 block) {
        if (block == image->parts[part].cached_block)
                return 0;


        if (io_read (image->parts[part].dec_buffer, 0x8000, image,
                     image->parts[part].offset +
                     image->parts[part].data_offset + (u64)(0x8000) * (u64)(block))
            != 0x8000) {
                //AfxMessageBox("decrypt read");
                return -1;
        }

        AES_cbc_encrypt (&image->parts[part].dec_buffer[0x400],
                         image->parts[part].cache, 0x7c00,
                         &image->parts[part].key,
                         &image->parts[part].dec_buffer[0x3d0], AES_DECRYPT);

        image->parts[part].cached_block = block;

        return 0;
}

size_t CWIIDisc::io_read_part (unsigned char *ptr, size_t size, struct image_file *image,
                     u32 part, u64 offset) {
        u32 block = (u32)(offset / (u64)(0x7c00));
        u32 cache_offset = (u32)(offset % (u64)(0x7c00));
        u32 cache_size;
        unsigned char *dst = ptr;

        if (!image->parts[part].is_encrypted)
                return io_read (ptr, size, image,
                                image->parts[part].offset + offset);

        while (size) {
                if (decrypt_block (image, part, block))
                        return (dst - ptr);

                cache_size = size;
                if (cache_size + cache_offset > 0x7c00)
                        cache_size = 0x7c00 - cache_offset;

                memcpy (dst, image->parts[part].cache + cache_offset,
                        cache_size);
                dst += cache_size;
                size -= cache_size;
                cache_offset = 0;

                block++;
        }

        return dst - ptr;
}



unsigned int CWIIDisc::CountBlocksUsed()
{
	unsigned int	nRetVal = 0;;
	u64				nBlock = 0;
	u64				i = 0;
	unsigned char	cLastBlock = 0x01;

	//AddToLog("------------------------------------------------------------------------------");
	for ( i =0; i < (nImageSize / (u64)(0x8000)); i++)
	{
		nRetVal += pFreeTable[i];
		if (cLastBlock!=pFreeTable[i])
		{
			// change so show
			//if (1==cLastBlock)
			//{
			//	//AddToLog("Marked Content", nBlock * (u64)(0x8000), i*(u64)(0x8000) - 1, (i-nBlock)*32);
			//}
			//else
			//{
			//	//AddToLog("Empty Content", nBlock * (u64)(0x8000), i*(u64)(0x8000) - 1, (i-nBlock)*32);
			//}
			nBlock = i;
			cLastBlock = pFreeTable[i];
		}

	}
	// output the final range
	/*if (1==cLastBlock)
	{
		AddToLog("Marked Content", nBlock *(u64)(0x8000), nImageSize - 1, (i-nBlock)*32);
	}
	else
	{
		AddToLog("Empty Content", nBlock * (u64)(0x8000), nImageSize - 1, (i-nBlock)*32);
	}
	AddToLog("------------------------------------------------------------------------------");*/

	return nRetVal;
}

//BOOL CWIIDisc::CleanupISO(CString csFileIn, CString csFileOut, int nMode, int nHeaderMode)
//{
//	FILE *	fIn = NULL;
//	FILE *  fOut = NULL;
//	FILE *	fOutDif = NULL;
//	
//	BOOL		bStatus = TRUE; // Used for error checking on read/write
//    MSG msg;
//	int x = 0;
//	
//	unsigned char inData[0x8000];
//	
//	
//	CString csDiffName;
//	
//	csDiffName = csFileOut;
//	
//	if (1!=nMode)
//	{
//		// the passed name is the save file, while if mode = 1 it's the
//		// dif filename already
//		// now check and replace the .iso if necessary
//		if (-1==csDiffName.Find(".iso",0))
//		{
//			// not found so append
//			csDiffName += ".dif";
//		}
//		else
//		{
//			// replace it
//			csDiffName.Replace(".iso", ".dif");
//		}
//	}
//	// now open files depending on the passed parameter
//	
//	switch(nMode)
//	{
//	case 0:
//		// try and create the output file first
//		fOut = fopen(csFileOut, "wb");
//		
//		if (NULL==fOut)
//		{
//			AddToLog("Unable to create save filename");
//			return FALSE;
//		}
//		break;
//	case 1:
//		// now open the dif file only
//		
//		fOutDif = fopen(csDiffName, "wb");
//		if (NULL==fOutDif)
//		{
//			AddToLog("Unable to create dif filename");
//			return FALSE;
//		}
//		break;
//	case 2:
//		// try and create the output file first
//		fOut = fopen(csFileOut, "wb");
//		
//		if (NULL==fOut)
//		{
//			AddToLog("Unable to create save filename");
//			return FALSE;
//		}
//		// now open the dif file as well
//		fOutDif = fopen(csDiffName, "wb");
//		if (NULL==fOutDif)
//		{
//			AddToLog("Unable to create dif filename");
//			// close the other output file
//			fclose(fOut);
//			return FALSE;
//		}
//		break;
//	default:
//		// non-standard value passed - so return with error
//		return FALSE;
//		break;
//	}
//	
//	//CProgressBox * pProgressBox;
//	
//	/*pProgressBox = new CProgressBox(m_pParent); 
//	
//	pProgressBox->Create(IDD_PROGRESSBOX);
//	
//	pProgressBox->ShowWindow(SW_SHOW);
//	pProgressBox->SetRange(0, (int)((nImageSize / (u64)(0x8000))));
//	
//	pProgressBox->SetPosition(0);*/
//	
//	CString csTempString;
//	
//	switch(nMode)
//	{
//	case 0:
//		csTempString.Format("Saving file: %s", csFileOut);
//		break;
//	case 1:
//		csTempString.Format("Saving file: %s", csDiffName);
//		break;
//	case 2:
//		csTempString.Format("Saving files: %s\n and %s", csFileOut, csDiffName);
//		break;
//		// no need for a default as would have been rejected at the earlier switch statement
//	}
//	//pProgressBox->SetWindowMessage(csTempString);
//	fIn = fopen(csFileIn, "rb");
//	// open the in and out files
//	// read the inblock of 32K
//	// check to see if we have to write it -  allow for bigger discs now
//	// as well as smaller ones
//	for( unsigned int i =0;
//		((i < (nImageSize/ (u64)(0x8000)))&&(!feof(fIn)));
//		i++)
//	{
//		
//		bStatus *= (0x8000==fread(inData, 1, 0x8000, fIn));
//		if (0x01==pFreeTable[i])
//		{
//			// block is marked as used so
//			switch(nMode)
//			{
//			case 0:
//				bStatus *= (0x8000==fwrite(inData, 1, 0x8000, fOut));
//				break;
//			case 1:
//				bStatus *= (0x0001==fwrite(pBlankSector0, 1, 1, fOutDif));
//				break;
//			case 2:
//				bStatus *= (0x8000==fwrite(inData, 1, 0x8000, fOut));
//				bStatus *= (0x0001==fwrite(pBlankSector0, 1, 1, fOutDif));
//				break;
//			}
//		}
//		else
//		{
//			// empty block so.......
//
//			switch(nMode)
//			{
//			case 0:
//				if (1==nHeaderMode)
//				{
//					// change back to 1.0 version.
//					// As it was pretty trivial for N to check the SHA tables then it seems
//					// pointless including them at the cost of 1k per sector
//					bStatus *= (0x8000==fwrite(pBlankSector, 1, 0x8000, fOut));
//				}
//				else
//				{
//					// 1.0a version
//					bStatus *= (0x0400==fwrite(inData, 1, 0x0400, fOut));
//					bStatus *= (0x7c00==fwrite(pBlankSector, 1, 0x7c00, fOut));
//				}
//				break;
//			case 1:
//				// now create the Dif file by writing out 0s then
//				// the Difd data
//				bStatus *= (0x0001==fwrite(pBlankSector, 1, 1, fOutDif));
//				bStatus *= (0x8000==fwrite(inData, 1, 0x8000, fOutDif));
//				break;
//			case 2:
//				if (1==nHeaderMode)
//				{
//					// change back to 1.0 version.
//					// As it was pretty trivial for N to check the SHA tables then it seems
//					// pointless including them at the cost of 1k per sector
//					bStatus *= (0x8000==fwrite(pBlankSector, 1, 0x8000, fOut));
//				}
//				else
//				{
//					// 1.0a version
//					bStatus *= (0x0400==fwrite(inData, 1, 0x0400, fOut));
//					bStatus *= (0x7c00==fwrite(pBlankSector, 1, 0x7c00, fOut));
//				}
//
//				bStatus *= (0x0001==fwrite(pBlankSector, 1, 1, fOutDif));
//				bStatus *= (0x8000==fwrite(inData, 1, 0x8000, fOutDif));
//				break;
//			}
//		}
//		//pProgressBox->SetPosition(i);
//		// do the message pump thang
//		
//        if (PeekMessage(&msg,
//            NULL,
//            0,
//            0,
//            PM_REMOVE))
//        {
//            // PeekMessage has found a message--process it 
//            if (msg.message != WM_CANCELLED)
//            {
//                TranslateMessage(&msg); // Translate virt. key codes 
//                DispatchMessage(&msg);  // Dispatch msg. to window 
//            }
//			else
//			{
//				// quit message received - simply exit
//				
//				AddToLog("Save cancelled");
//				//delete pProgressBox;
//				if (NULL!=fOutDif)
//				{
//					fclose(fOutDif);
//				}
//				if (NULL!=fOut)
//				{
//					fclose(fOut);
//				}
//				fclose(fIn);
//				return FALSE;
//			}
//        }
//		if (FALSE==bStatus)
//		{
//			// error in read or write - don't care where, just exit with error
//			//delete pProgressBox;
//			if (NULL!=fOutDif)
//			{
//				fclose(fOutDif);
//			}
//			if (NULL!=fOut)
//			{
//				fclose(fOut);
//			}
//			fclose(fIn);
//			return FALSE;
//		}
//		
//	}
//	//delete pProgressBox;
//	if (NULL!=fOutDif)
//	{
//		fclose(fOutDif);
//	}
//	if (NULL!=fOut)
//	{
//		fclose(fOut);
//	}
//	fclose(fIn);
//	return TRUE;
//}

void CWIIDisc::MarkAsUsed(u64 nOffset, u64 nSize)
{
		u64 nStartValue = nOffset;
		u64 nEndValue = nOffset + nSize;
		while((nStartValue < nEndValue)&&
			  (nStartValue < ((u64)(4699979776) * (u64)(2))))
		{

			pFreeTable[nStartValue / (u64)(0x8000)] = 1;
			nStartValue = nStartValue + ((u64)(0x8000));
		}

}
void CWIIDisc::MarkAsUsedDC(u64 nPartOffset, u64 nOffset, u64 nSize, BOOL bIsEncrypted)
{
	u64 nTempOffset;
	u64 nTempSize;
	
	if (TRUE==bIsEncrypted)
	{
		// the offset and size relate to the decrypted file so.........
		// we need to change the values to accomodate the 0x400 bytes of crypto data
		
		nTempOffset = nOffset / (u64)(0x7c00);
		nTempOffset = nTempOffset * ((u64)(0x8000));
		nTempOffset += nPartOffset;
		
		nTempSize = nSize / (u64)(0x7c00);
		nTempSize = (nTempSize + 1) * ((u64)(0x8000));
		
		// add on the offset in the first nblock for the case where data straddles blocks
		
		nTempSize += nOffset % (u64)(0x7c00);
	}
	else
	{
		// unencrypted - we use the actual offsets
		nTempOffset = nPartOffset + nOffset;
		nTempSize = nSize;
	}
	MarkAsUsed(nTempOffset, nTempSize);

}


//void CWIIDisc::AddToLog(CString csText)
//{
//	//m_pParent->AddToLog(csText);
//}
//void CWIIDisc::AddToLog(CString csText, u64 nValue)
//{
//	//m_pParent->AddToLog(csText, nValue);
//}

//void CWIIDisc::AddToLog(CString csText, u64 nValue1, u64 nValue2)
//{/*
//	CString csText1;
//	csText1.Format("%s [0x%I64X], [0x%I64X]", csText, nValue1, nValue2);*/
//	//m_pParent->AddToLog(csText1);
//}

//void CWIIDisc::AddToLog(CString csText, u64 nValue1, u64 nValue2, u64 nValue3)
//{
//	CString csText1;
//	csText1.Format("%s [0x%I64X], [0x%I64X] [%I64d K]", csText, nValue1, nValue2, nValue3);
//	//m_pParent->AddToLog(csText1);
//}

void CWIIDisc::Reset()
{
	//set them all to clear first
	memset(pFreeTable, 0, ((4699979776L / 32768L)* 2L));
	// then set the header size to used
	MarkAsUsed(0,0x50000);
	//hDisc = NULL;
	/*for(int i=0;i<20;i++)
	{
		hPartition[i]=NULL;
	}*/

	// then clear the decrypt key
	u8 key[16];

	memset(key,0,16);

	AES_KEY nKey;

	memset(&nKey, 0, sizeof(AES_KEY));
	AES_set_decrypt_key (key, 128, &nKey);
}

BOOL CWIIDisc::SaveDecryptedFile(const char* csDestinationFilename,  struct image_file *image,
								u32 part, u64 nFileOffset, u64 nFileSize, BOOL bOverrideEncrypt)
{
	FILE * fOut;

		u32 block = (u32)(nFileOffset / (u64)(0x7c00));
        u32 cache_offset = (u32)(nFileOffset % (u64)(0x7c00));
        u64 cache_size;


        unsigned char cBuffer[0x8000];

		fOut = fopen(csDestinationFilename, "wb");

        if ((!image->parts[part].is_encrypted)||
			(TRUE==bOverrideEncrypt))
		{
			if (-1==_lseeki64 (image->fp, nFileOffset, SEEK_SET)) {
				//DWORD x = GetLastError();
				//AfxMessageBox("io_seek");
				return -1;
			}

			while(nFileSize)
			{
				cache_size = nFileSize;
				
				if (cache_size  > 0x8000)
					cache_size = 0x8000;
				
				
				_read(image->fp, &cBuffer[0], (u32)(cache_size));
				
				fwrite(cBuffer, 1, (u32)(cache_size), fOut);
				
				nFileSize -= cache_size;

			}
		}
		else
		{
			while (nFileSize) {
				    if (decrypt_block (image, part, block))
					{
						fclose(fOut);
					        return FALSE;
					}

					cache_size = nFileSize;
					if (cache_size + cache_offset > 0x7c00)
						    cache_size = 0x7c00 - cache_offset;

					if (cache_size!=fwrite(image->parts[part].cache + cache_offset, 1,
						    (u32)(cache_size), fOut))
					{
						//AddToLog("Error writing file");
						fclose(fOut);
						return FALSE;
					}
 
					nFileSize -= cache_size;
					cache_offset = 0;

					block++;
			}
		}
		fclose (fOut);

	return TRUE;
}

//BOOL CWIIDisc::LoadDecryptedFile(CString csDestinationFilename,  struct image_file *image,
//								 u32 part, u64 nFileOffset, u64 nFileSize, int nFSTReference)
//{
//	FILE * fIn;
//	u32		nImageSize;	
//	u64		nfImageSize;
//	u8 *	pBootBin = (unsigned char *) calloc(0x440,1);
//	u8 *	pPartData ;
//	u64		nFreeSpaceStart;
//	u32		nExtraData;
//	u32		nExtraDataBlocks;
//
//
//	// create a 64 cluster buffer for the file
//	
//	fIn = fopen(csDestinationFilename, "rb");
//	
//    if (NULL==fIn)
//	{
//		AddToLog("Error opening file");
//		return FALSE;
//	}
//	
//	// get the size of the file we are trying to load
//
//	nfImageSize = _lseeki64(fIn->_file, 0L, SEEK_END);
//	nImageSize = (u32) nfImageSize;
//
//	// pointer back to the start
//	_lseeki64(fIn->_file, 0L, SEEK_SET);
//	
//	
//	// now get the filesize we are trying to load and make sure it is smaller
//	// or the same size as the one we are trying to replace if so then a simple replace
//	if (nFileSize >= nImageSize)
//	{
//		// simple replace
//		// now need to change the boot.bin if one if fst.bin or main.dol were changed
//		
//		if (nFileSize!=nfImageSize)
//		{
//			// we have a different sized file being put in
//			// this is obviously smaller but will require a tweak to one of the file
//			// entries
//			if (0<nFSTReference)
//			{
//				// normal file so change the FST.BIN
//				u8 * pFSTBin = (unsigned char *) calloc((u32)(image->parts[part].header.fst_size),1);
//			
//				io_read_part(pFSTBin, (u32)(image->parts[part].header.fst_size), image, part, image->parts[part].header.fst_offset);
//			
//				// alter the entry for the passed FST Reference
//				nFSTReference = nFSTReference * 0x0c;
//			
//				// update the length for the file
//				Write32(pFSTBin + nFSTReference + 0x08L , nImageSize);
//			
//				// write out the FST.BIN
//				wii_write_data_file(image, part, image->parts[part].header.fst_offset, (u32)(image->parts[part].header.fst_size), pFSTBin);
//				
//				// write it out
//				wii_write_data_file(image, part, nFileOffset, nImageSize, NULL, fIn);
//
//			}
//			else
//			{
//				switch(nFSTReference)
//				{
//				case 0:
//					// - one of the files that should ALWAYS be the correct size
//					AfxMessageBox("Error as file sizes do not match and they MUST for boot.bin and bi2.bin");
//					fclose(fIn);
//					free(pBootBin);
//					return FALSE;
//					break;
//				case -1:
//					// FST
//					io_read_part(pBootBin, 0x440, image, part, 0);
//					
//					// update the settings for the FST.BIN entry
//					// this has to be rounded to the nearest 4 so.....
//					if (0!=(nImageSize%4))
//					{
//						nImageSize = nImageSize + (4 - nImageSize%4);
//					}
//					Write32(pBootBin + 0x428L, (u32)(nImageSize >>2));
//					Write32(pBootBin + 0x42CL, (u32)(nImageSize >>2));
//					// now write it out
//					wii_write_data_file(image, part, 0, 0x440, pBootBin);
//	
//					break;
//				case -2:
//					// main.dol - don't have to do anything
//					break;
//				case -3:
//					// apploader - don't have to do anything
//					break;
//				case -4:
//					// partition.bin
//					AfxMessageBox("Error as partition.bin MUST be 0x20000 bytes in size");
//					fclose(fIn);
//					free(pBootBin);
//					return FALSE;
//
//					break;
//				case -5:
//					AfxMessageBox("Error as tmd.bin MUST be same size");
//					fclose(fIn);
//					free(pBootBin);
//					return FALSE;
//					break;
//				case -6:
//					AfxMessageBox("Error as cert.bin MUST be same size");
//					fclose(fIn);
//					free(pBootBin);
//					return FALSE;
//					break;
//				case -7:
//					AfxMessageBox("Error as h3.bin MUST be 0x18000 bytes in size");
//					fclose(fIn);
//					free(pBootBin);
//					return FALSE;
//					break;
//				default:
//					AfxMessageBox("Unknown file reference passed");
//					fclose(fIn);
//
//					free(pBootBin);
//					return FALSE;
//					break;
//				}
//				// now write it out
//				wii_write_data_file(image, part, nFileOffset, nImageSize, NULL, fIn);
//			}
//		}
//		else
//		{
//			// Equal sized file so need to check for the special cases
//			if (0>nFSTReference)
//			{
//				switch(nFSTReference)
//				{
//				case -1:
//				case -2:
//				case -3:
//					// simple write as files are the same size
//					wii_write_data_file(image, part, nFileOffset, nImageSize, NULL, fIn);
//					break;
//				case -4:
//					// Partition.bin
//					// it's a direct write
//					pPartData = (u8 *)calloc(1,(unsigned int)nFileSize);
//					
//					fread(pPartData,1,(unsigned int)nFileSize, fIn);
//					DiscWriteDirect(image, image->parts[part].offset, pPartData, (unsigned int)nFileSize);
//					free(pPartData);
//					break;
//				case -5:
//					// tmd.bin;
//				case -6:
//					// cert.bin
//				case -7:
//					// h3.bin
//
//					// same for all 3
//					pPartData = (u8 *)calloc(1,(unsigned int)nFileSize);
//					
//					fread(pPartData,1,(unsigned int)nFileSize, fIn);
//					DiscWriteDirect(image, image->parts[part].offset + nFileOffset, pPartData, (unsigned int)nFileSize);
//					free(pPartData);
//
//					break;
//				default:
//					AddToLog("Unknown file reference passed");
//					break;
//				}
//			}
//			else
//			{
//				// simple write as files are the same size
//				wii_write_data_file(image, part, nFileOffset, nImageSize, NULL, fIn);
//			}
//		}
//		
//	}
//	else
//	{
//		// Alternatively just have to update the FST or boot.bin depending on the file we want to change
//		// this will depend on whether the passed index is
//		// -ve = Partition data,
//		// 0 = given by boot.bin,
//		// +ve = normal file
//		
//		// need to find some free space in the partition first
//		nFreeSpaceStart = FindRequiredFreeSpaceInPartition(image, part, nImageSize);
//		
//		if (0==nFreeSpaceStart)
//		{
//			// no free space - so cant do it
//			AfxMessageBox("Unable to find free space to add the file :(");
//			fclose(fIn);
//
//			free(pBootBin);
//			return FALSE;
//		}
//		
//		// depending on the passed offset we then need to modify either the
//		// fst.bin or the boot.bin
//		if (0<nFSTReference)
//		{
//			// normal one - so read out the fst and change the values for the relevant pointer
//			// before writing it out
//			u8 * pFSTBin = (unsigned char *) calloc((u32)(image->parts[part].header.fst_size),1);
//			
//			io_read_part(pFSTBin, (u32)(image->parts[part].header.fst_size), image, part, image->parts[part].header.fst_offset);
//			
//			// alter the entry for the passed FST Reference
//			nFSTReference = nFSTReference * 0x0c;
//			
//			// update the offset for this file
//			Write32(pFSTBin + nFSTReference + 0x04L, u32 (nFreeSpaceStart >> 2));
//			// update the length for the file
//			Write32(pFSTBin + nFSTReference + 0x08L , nImageSize);
//			
//			// write out the FST.BIN
//			wii_write_data_file(image, part, image->parts[part].header.fst_offset, (u32)(image->parts[part].header.fst_size), pFSTBin);
//			
//			// now write data file out
//			wii_write_data_file(image, part, nFreeSpaceStart, nImageSize, NULL, fIn);
//			
//		}
//		else
//		{
//			
//			switch(nFSTReference)
//			{
//			case -1: // FST.BIN
//				// change the boot.bin file too and write that out
//				io_read_part(pBootBin, 0x440, image, part, 0);
//
//				// update the settings for the FST.BIN entry
//				// this has to be rounded to the nearest 4 so.....
//				if (0!=(nImageSize%4))
//				{
//					nImageSize = nImageSize + (4 - nImageSize%4);
//				}
//				
//				// update the settings for the FST.BIN entry
//				Write32(pBootBin + 0x424L, u32 (nFreeSpaceStart >> 2)); 
//				Write32(pBootBin + 0x428L, (u32)(nImageSize >> 2));
//				Write32(pBootBin + 0x42CL, (u32)(nImageSize >> 2));
//				
//				// now write it out
//				wii_write_data_file(image, part, 0, 0x440, pBootBin);
//				
//				// now write it out
//				wii_write_data_file(image, part, nFreeSpaceStart, nImageSize, NULL, fIn);
//				
//				
//				break;
//			case -2: // Main.DOL
//				// change the boot.bin file too and write that out
//				io_read_part(pBootBin, 0x440, image, part, 0);
//				
//				// update the settings for the main.dol entry
//				Write32(pBootBin + 0x420L, u32 (nFreeSpaceStart >> 2)); 
//				
//				// now write it out
//				wii_write_data_file(image, part, 0, 0x440, pBootBin);
//				
//				// now write main.dol out
//				wii_write_data_file(image, part, nFreeSpaceStart, nImageSize, NULL, fIn);
//				
//				
//				break;
//			case -3: // Apploader.img - now this is fun! as we have to
//				// move the main.dol and potentially fst.bin too  too otherwise they would be overwritten
//				// also what happens if the data for those two has already been moved
//				// aaaargh
//				
//				// check to see what we have to move
//				// by calculating the amount of extra data we are trying to stuff in
//				nExtraData = (u32)(nImageSize - image->parts[part].appldr_size);
//				
//				nExtraDataBlocks = 1 + ((nImageSize - (u32)(image->parts[part].appldr_size)) / 0x7c00);
//				
//				// check to see if we have that much free at the end of the area
//				// or do we need to try and overwrite
//				if (TRUE==CheckForFreeSpace(image, part,image->parts[part].appldr_size + 0x2440 ,nExtraDataBlocks))
//				{
//					// we have enough space after the current apploader - already moved the main.dol?
//					// so just need to write it out.
//					wii_write_data_file(image, part, 0x2440, nImageSize, NULL, fIn);
//					
//				}
//				else
//				{
//					// check if we can get by with moving the main.dol
//					if (nExtraData > image->parts[part].header.dol_size)
//					{
//						// don't really want to be playing around here as we potentially can get
//						// overwrites of all sorts of data
//						AfxMessageBox("Cannot guarantee writing data correctly\nI blame nargels");
//						AddToLog("Cannot guarantee a good write of apploader");
//						fclose(fIn);
//
//						free(pBootBin);
//						return FALSE;
//					}
//					else
//					{
//						// "just" need to move main.dol
//						u8 * pMainDol = (u8 *) calloc((u32)(image->parts[part].header.dol_size),1);
//						
//						io_read_part(pMainDol, (u32)(image->parts[part].header.dol_size), image, part, image->parts[part].header.dol_offset);
//						
//						// try and get some free space for it
//						nFreeSpaceStart = FindRequiredFreeSpaceInPartition(image, part, (u32)(image->parts[part].header.dol_size));
//						
//						// now unmark the original dol area
//						MarkAsUnused(image->parts[part].offset+image->parts[part].data_offset+(((image->parts[part].header.dol_offset)/0x7c00)*0x8000),
//							image->parts[part].header.dol_size);
//						
//						if ((0!=nFreeSpaceStart)&&
//							(TRUE==CheckForFreeSpace(image, part,image->parts[part].appldr_size + 0x2440 ,nExtraDataBlocks)))
//						{
//							// got space so write it out
//							wii_write_data_file(image, part, nFreeSpaceStart, (u32)(image->parts[part].header.dol_size), pMainDol);
//							
//							// now do the boot.bin file too
//							io_read_part(pBootBin, 0x440, image, part, 0);
//							
//							// update the settings for the boot.BIN entry
//							Write32(pBootBin + 0x420L, u32 (nFreeSpaceStart >> 2)); 
//							
//							// now write it out
//							wii_write_data_file(image, part, 0, 0x440, pBootBin);
//							
//							// now write out the apploader - we don't need to change any other data
//							// as the size is inside the apploader
//							wii_write_data_file(image, part, 0x2440, nImageSize, NULL, fIn);
//							
//						}
//						else
//						{
//							// cannot do it :(
//							AfxMessageBox("Unable to move the main.dol and find enough space for the apploader.");
//							AddToLog("Unable to add larger apploader");
//							free(pMainDol);
//							free(pBootBin);
//							fclose(fIn);
//
//							return FALSE;
//						}
//						
//						
//					}
//				}
//				break;
//			default:
//				// Unable to do these as they are set sizes and lengths
//				// boot.bin and bi2.bin
//				// partition.bin
//				AfxMessageBox("Unable to change that file as it is a set size\nin the disc image");
//				AddToLog("Unable to change set size file");
//				free(pBootBin);
//				fclose(fIn);
//				return FALSE;
//				break;
//			}
//			
//		}
//	}
//
//	
//	// free the memory we allocated
//	free(pBootBin);
//	fclose(fIn);
//	
//	return TRUE;
//}

BOOL CWIIDisc::CheckAndLoadKey(BOOL bLoadCrypto, struct image_file *image)
{
	FILE * fp_key;
	static u8 LoadedKey[16];

	if (TRUE==bLoadCrypto)
	{
		fp_key = fopen (this->pathToCommonKey, "rb");
	
		if (fp_key == NULL) {
			//AfxMessageBox("Unable to open key.bin");
			return FALSE;
		}
	
		if (16 != fread (LoadedKey, 1, 16, fp_key)) {
			fclose (fp_key);
			//AfxMessageBox("key.bin not 16 bytes in size");
			return FALSE;
		}
	
		fclose (fp_key);

		// now check to see if it's the right key
		// as we don't want to embed the key value in here then lets cheat a little ;)
		// by checking the Xor'd difference values
		if	((0x0F!=(LoadedKey[0]^LoadedKey[1]))||
			(0xCE!=(LoadedKey[1]^LoadedKey[2]))||
			(0x08!=(LoadedKey[2]^LoadedKey[3]))||
			(0x7C!=(LoadedKey[3]^LoadedKey[4]))||
			(0xDB!=(LoadedKey[4]^LoadedKey[5]))||
			(0x16!=(LoadedKey[5]^LoadedKey[6]))||
			(0x77!=(LoadedKey[6]^LoadedKey[7]))||
			(0xAC!=(LoadedKey[7]^LoadedKey[8]))||
			(0x91!=(LoadedKey[8]^LoadedKey[9]))||
			(0x1C!=(LoadedKey[9]^LoadedKey[10]))||
			(0x80!=(LoadedKey[10]^LoadedKey[11]))||
			(0x36!=(LoadedKey[11]^LoadedKey[12]))||
			(0xF2!=(LoadedKey[12]^LoadedKey[13]))||
			(0x2B!=(LoadedKey[13]^LoadedKey[14]))||
			(0x5D!=(LoadedKey[14]^LoadedKey[15])))
		{
			// handle the Korean key, in case it ever gets found
			/*if (AfxMessageBox("Doesn't seem to be the correct key.bin\nDo you want to use anyways??",
				MB_YESNO|MB_ICONSTOP|MB_DEFBUTTON2)==IDNO)
			{
				return FALSE;
			}*/
			return FALSE;
		}
		AES_set_decrypt_key (LoadedKey, 128, &image->key);
	}
	return TRUE;
}


//////////////////////////////////////////////////////////////////////
// Recreate the original data from a DIF file and a compress file   //
//////////////////////////////////////////////////////////////////////
//BOOL CWIIDisc::RecreateOriginalFile(CString csScrubbedName, CString csDIFName, CString csOutName)
//{
//	FILE *	fInCompress = NULL;
//	FILE *	fInDif = NULL;
//	FILE *  fOut = NULL;
//	u64 nFileSize;
//    
//	unsigned long	nRead = 0L;
//	MSG msg;
//	int i = 0;
//	
//	unsigned char inData[0x8000];
//	unsigned char inDifData[0x8000];
//
//
//	fInCompress = fopen(csScrubbedName, "rb");
//	fInDif = fopen(csDIFName, "rb");
//
//	if ((NULL==fInCompress)||
//		(NULL==fInDif))
//	{
//		// error opning one of the files
//		if (NULL!=fInCompress)
//		{
//			fclose(fInCompress);
//		}
//		if (NULL!=fInDif)
//		{
//			fclose(fInDif);
//		}
//		return FALSE;
//	}
//	
//	// try and create the output file
//	if (NULL==(fOut = fopen(csOutName, "wb")))
//	{
//		// error - close open and return
//		fclose(fInDif);
//		fclose(fInCompress);
//		return FALSE;
//	}
//
//	// get the input filesize
//	nFileSize = _lseeki64(fInCompress->_file, 0L, SEEK_END);
//	_lseeki64(fInCompress->_file, 0L, SEEK_SET);
//
//
//	// create the progress box
//	//CProgressBox * pProgressBox;
//	///*
//	//pProgressBox = new CProgressBox(m_pParent); 
//	//
//	//pProgressBox->Create(IDD_PROGRESSBOX);
//	//
//	//pProgressBox->ShowWindow(SW_SHOW);
//	//pProgressBox->SetRange(0, (int)((nFileSize / (u64)(0x8000))));
//	//
//	//pProgressBox->SetPosition(0);
//	//
//	//CString csTempString;
//
//	//csTempString.Format("Creating file %s", csOutName);
//
//	//pProgressBox->SetWindowMessage(csTempString);*/
//
//	i = 0;
//	while(!feof(fInCompress))
//	{
//		// read in a block of data
//		nRead = fread(inData,1,0x8000, fInCompress);
//		if (0x8000==nRead)
//		{
//			
//			// read in a byte from the DIFF file
//			fread(inDifData, 1,1,fInDif);
//			// if DIFF file flagged as 'change data' then
//			if (0xFF==inDifData[0])
//			{
//				// read in block of data
//				fread(inDifData, 1, 0x8000, fInDif);
//				// ouput it
//				fwrite(inDifData, 1, 0x8000, fOut);
//			}
//			else
//			{
//				// just need to send the input from the compressed to the output
//				fwrite(inData, 1, 0x8000, fOut);
//			}
//		}
//		else
//		{
//			// we've read to the end
//		}
//
//		i++;
//		
//		// message pump again
//		//pProgressBox->SetPosition(i);
//		// do the message pump thang
//		
//        if (PeekMessage(&msg,
//            NULL,
//            0,
//            0,
//            PM_REMOVE))
//        {
//            // PeekMessage has found a message--process it 
//            if (msg.message != WM_CANCELLED)
//            {
//                TranslateMessage(&msg); // Translate virt. key codes 
//                DispatchMessage(&msg);  // Dispatch msg. to window 
//            }
//			else
//			{
//				// quit message received - simply exit
//				
//				AddToLog("Creation cancelled");
//				//delete pProgressBox;
//				if (NULL!=fInDif)
//				{
//					fclose(fInDif);
//				}
//				if (NULL!=fInCompress)
//				{
//					fclose(fInCompress);
//				}
//				if (NULL!=fOut)
//				{
//					fclose(fOut);
//				}
//				return FALSE;
//			}
//        }
//
//	}
//	//delete pProgressBox;
//	fclose(fInDif);
//	fclose(fInCompress);
//	fclose(fOut);
//	return TRUE;
//	
//}
////////////////////////////////////////////////////////////////////////
// here we find the free space, modify the fst.bin and do some other  //
// bit buggering to add 5 files to the image                          //
// As these are filled with blanks then they compress well even after //
// encrypting                                                         //
////////////////////////////////////////////////////////////////////////

//BOOL CWIIDisc::TruchaScrub(image_file * image, unsigned int nPartition)
//{
//		unsigned char * pFST = NULL;
//		unsigned char * pFSTCopy = NULL;
//		unsigned char * pBootBin = NULL;
//		unsigned char * pFSTDummy = NULL;
//		FILE * fOut;
//
//		int z = 0;
//
//		u64 nFSTOldSize;
//		u64 nFSTNewSize;
//		u32 nFSTNewDiscSize;
//
//		//// find the free space size
//		u64 nOffset;
//		u64 nSize;
//		u64 nSizeForFiles;
//
//		FindFreeSpaceInPartition(image->parts[nPartition].offset, &nOffset, &nSize);
//
//		// now need to subtract the partition info from the start
//		nOffset = nOffset -(image->parts[nPartition].offset+image->parts[nPartition].data_offset);
//
//		nFSTOldSize = (image->parts[nPartition].header.fst_size);
//		nFSTNewSize = (image->parts[nPartition].header.fst_size) + (0x0c + 0x0c)*5; //5 extra entries plus extra strings
//		nFSTNewDiscSize = u32 (nFSTNewSize >> 2);
//
//		// now follows an example bit of code on how to put whatever you like
//		// onto a TRUCHA disc by a three stage process
//
//		/////////////////////////////////////////////////////////////////////////
//		// Step 1 = prepare a modified boot.bin that points to where the new
//		// fst.bin will live
//		/////////////////////////////////////////////////////////////////////////
//
//		// get the boot.bin file out and then save it
//		// with modified values for a 'fixed' FST
//		pBootBin = (unsigned char *) malloc(0x440);
//		io_read_part(pBootBin, 0x440, image, nPartition, 0);
//
//		Write32(pBootBin + 0x424L, u32 (nOffset >> 2)); 
//		Write32(pBootBin + 0x428L, nFSTNewDiscSize);
//		Write32(pBootBin + 0x42CL, nFSTNewDiscSize);
//		// also update the offset to point to the new FST location after the second write
//		// save the boot.bin
//		fOut = fopen("ReplacementBoot.bin", "wb");
//		fwrite(pBootBin, 1, 0x440, fOut);
//		fclose(fOut);
//		/////////////////////////////////////////////////////////////////////////
//		// Step 2 = create a modified fst.bin that points to where we are going
//		// to store the fst.bin we really want to use in the new disc image
//		/////////////////////////////////////////////////////////////////////////
//
//		// now create the fst.bin we use to chuck our data into the correct place
//		// only two entries plus a file name
//		pFSTDummy = (unsigned char *) malloc((0x0C * 2) + 0x10);
//
//		// create the dummy FST
//		memset(pFSTDummy, 0, 0x28);
//		// standard start record
//		Write32(pFSTDummy, 0x01000000);
//		Write32(pFSTDummy + 0x04L, 0x00000000);
//		Write32(pFSTDummy + 0x08L, 0x00000002);
//
//		// now the data entry
//		Write32(pFSTDummy + 0x0C, 0x00000000);			// file , first string table entry
//		Write32(pFSTDummy + 0x10, u32 (nOffset >> 2));	// start location
//		Write32(pFSTDummy + 0x14, (u32)(nFSTNewSize));		// size of new FST
//
//		// now the string table entry
//		memcpy(pFSTDummy + 0x18L, "FakeFSTGoesHere", 0x0F);
//
//		fOut = fopen("FakeFST.bin", "wb");
//		fwrite(pFSTDummy, 1, 0x28, fOut);
//		fclose(fOut);
//
//		/////////////////////////////////////////////////////////////////////////
//		// Step 3 = create the new fst.bin we really want to use
//		// in this case we are going to add 5 files into the image that will be
//		// filled with blank space as even when encrypted the data will compress
//		/////////////////////////////////////////////////////////////////////////
//
//
//		// get the fst.bin from the image file
//		pFST = (unsigned char *) malloc((u32)(nFSTOldSize));
//
//		io_read_part(pFST, (u32)(nFSTOldSize), image, nPartition, image->parts[nPartition].header.fst_offset);
//		// allocate enough for the altered version
//		pFSTCopy = (unsigned char *)(malloc((u32)(nFSTNewSize))); // extra entry plus string chars
//
//		// clear it out first
//		memset(pFSTCopy, 0, (u32)(nFSTNewSize));
//		
//
//		// modify it to add extra files in the root directory using the name SCRUBPT1 to SCRUBPT5
//
//		unsigned int nFiles = be32(pFST + 8);
//
//		unsigned int nStringTableOffset = (nFiles  * 0x0c);
//
//		// copy the existing data
//		// Table first
//		memcpy(pFSTCopy, pFST, nStringTableOffset);
//		
//		// then the string table
//		memcpy(pFSTCopy + (nFiles+5)*0x0c, pFST + (nFiles *0x0c), (u32)(nFSTOldSize) - (nFiles * 0x0c));
//
//
//		// update the number of files in the copy by adding 5
//		Write32(pFSTCopy + 8, nFiles + 5);
//
//
//		// available space is now what was available minus the size of the new FST.BIN rounded up
//		// to nearest block
//		u64 nFSTTweak = (((nFSTNewSize / (u64)(0x7C00)) + 1) * (u64)(0x7C00));
//
//		nSize = nSize - nFSTTweak;
//
//		// save the number for later file creation
//
//		nSizeForFiles = nSize;
//
//		nOffset = nOffset + nFSTTweak;
//
//		// now loop around adding the correct data for each of the table entries
//		
//		u32 nFSTStringTableSize;
//
//		nFSTStringTableSize = (u32)(nFSTNewSize) - ((nFiles + 5) * 0x0C);
//
//		// now find where the existing data really ends by moving backwards from
//		// the end until we find a non 0 character
//		// this should stop fstreader getting it's knickers in a twist
//
//		u32 nStringPad = (u32)(nFSTNewSize) -1;
//
//		while (0==pFSTCopy[nStringPad])
//		{
//			nStringPad --;
//		}
//
//		// we are now at the first non 0 char
//		// so add two to give us the correct offset
//		nStringPad +=2;
//
//
//
//
//		for (z = 0; z < 5; z++)
//		{
//			// string pointer at end of table minus 5 entries plus whatever entry we are at
//			Write32(pFSTCopy + (nFiles + z)*0x0C, nStringPad - ((nFiles +5)*0x0C)+ (z * 0x0C));
//			
//			// then offset - value needs to be divided by 4 after adding on the block modified
//			// fst.bin ammendment
//		
//			Write32(pFSTCopy + (nFiles + z)*0x0C + 0x04, (u32)(nOffset >> 2));
//			
//			// then length - this will be either the max data size or
//			// the actual size depending on the data we have left to pad
//			if (nSize >= 0x3FFFFC00)
//			{
//				Write32(pFSTCopy + (nFiles + z)*0x0C + 0x08, 0x3FFFFC00);
//				nSize = nSize - 0x3FFFFC00;
//				nOffset = nOffset + 0x3FFFFC00;
//			}
//			else
//			{
//				u32 nTempSize;
//				nTempSize = (u32)(nSize);
//
//				Write32(pFSTCopy + (nFiles + z)*0x0C + 0x08, nTempSize);
//				nSize = 0;
//			}
//
//		
//			// add the string entry for the name "SCRUBPT1 to 5"
//			memcpy(&pFSTCopy[nStringPad + (z * 0x0C)], "SCRUBPX.DAT", 0x0B);
//			// then add the part number
//			pFSTCopy[nStringPad + (z * 0x0C) + 0x06] = '1'+ z;
//		}
//
//		// save the fst.bin
//
//		fOut = fopen("SCRUBBEDFST.bin", "wb");
//		fwrite(pFSTCopy, 1, (u32)(nFSTNewSize), fOut);
//		fclose(fOut);
//
//		/////////////////////////////////////////////////////////////////////////
//		// now we need to create the blank files
//		/////////////////////////////////////////////////////////////////////////
//
//		// create the progress bar etc.....
//		//CProgressBox * pProgressBox;
//	    MSG msg;
//		
//		//pProgressBox = new CProgressBox(m_pParent); 
//		//
//		//pProgressBox->Create(IDD_PROGRESSBOX);
//		//
//		//pProgressBox->ShowWindow(SW_SHOW);
//		//pProgressBox->SetRange(0,  (int)((nSizeForFiles / (u64)(0x7c00))));
//		//
//		//pProgressBox->SetPosition(0);
//		//pProgressBox->SetWindowMessage("Saving blank files");
//
//		int nPosition = 0;
//
//		for (z=1; z < 6 ; z++)
//		{
//			// create a file of the right size to pad it
//			CString csfName;
//			
//			csfName.Format("SCRUBP%d.DAT", z);
//			
//			if (0!=nSizeForFiles)
//			{
//				
//				if ((u64)0x3FFFFC00 <= nSizeForFiles)
//				{
//					if (1==z) // only create the first full file if we need to
//					{
//						fOut = fopen(csfName, "wb");
//						// full block
//						// output in 0x7c00 blocks as the figure must be a multiple of this
//						for(u32 x = 0; x < 33825; x ++)
//						{
//							fwrite(pBlankSector, 1, 0x7c00, fOut);
//							nPosition ++;
//							
//							//pProgressBox->SetPosition(nPosition);
//							// do the message pump thang
//							
//							if (PeekMessage(&msg,
//								NULL,
//								0,
//								0,
//								PM_REMOVE))
//							{
//								// PeekMessage has found a message--process it 
//								if (msg.message != WM_CANCELLED)
//								{
//									TranslateMessage(&msg); // Translate virt. key codes 
//									DispatchMessage(&msg);  // Dispatch msg. to window 
//								}
//								else
//								{
//									// quit message received - simply exit
//									
//									AddToLog("Save cancelled");
//									//delete pProgressBox;
//									if (NULL!=fOut)
//									{
//										fclose(fOut);
//									}
//									free (pBootBin);
//									free(pFST);
//									free(pFSTCopy);
//									free(pFSTDummy);
//									return FALSE;
//								}
//							}
//						}
//						fclose(fOut);
//					}
//					else
//					{
//						nPosition += 33825L;
//						//pProgressBox->SetPosition(nPosition);
//					}
//
//					nSizeForFiles = nSizeForFiles - (u64) 0x3FFFFC00;
//				}
//				else
//				{
//					fOut = fopen(csfName, "wb");
//					// partially filled block - must be at the end
//					// output in 0x7c00 blocks as the figure must be a multiple of this
//					for(u32 x = 0; x < nSizeForFiles; x +=0x7C00)
//					{
//						fwrite(pBlankSector, 1, 0x7c00, fOut);
//						nPosition ++;
//						//pProgressBox->SetPosition(nPosition);
//						
//						// do the message pump thang
//						
//						if (PeekMessage(&msg,
//							NULL,
//							0,
//							0,
//							PM_REMOVE))
//						{
//							// PeekMessage has found a message--process it 
//							if (msg.message != WM_CANCELLED)
//							{
//								TranslateMessage(&msg); // Translate virt. key codes 
//								DispatchMessage(&msg);  // Dispatch msg. to window 
//							}
//							else
//							{
//								// quit message received - simply exit
//								
//								AddToLog("Save cancelled");
//								//delete pProgressBox;
//								if (NULL!=fOut)
//								{
//									fclose(fOut);
//								}
//								free (pBootBin);
//								free(pFST);
//								free(pFSTCopy);
//								free(pFSTDummy);
//								return FALSE;
//							}
//						}
//					}
//					nSizeForFiles = 0;
//					fclose(fOut);
//				}
//				
//			}
//		}
//		//delete pProgressBox;
//		
//		// Phew! - now free up the memory
//		free (pBootBin);
//		free(pFST);
//		free(pFSTCopy);
//		free(pFSTDummy);
//		return TRUE;
//
//}

//void CWIIDisc::FindFreeSpaceInPartition(__int64 nPartOffset, u64 *pStart, u64 *pSize)
//{
//
//	// go through the data block table to find the first unused block starting
//	// at the offest provided and use that to search to the end of the data or
//	// the first marked.
//	// As WII games all use a (  << 2) implementation of size then we just need to
//	// used the supplied values divided by 4 for the returns
//
//	char cLastBlock = 1; // assume (!) that we have data at the partition start
//
//
//	*pSize = 0;
//	*pStart = 0;
//
//	for (u64 i = (nPartOffset / (u64)(0x8000)); i < (nImageSize / (u64)(0x8000)); i++)
//	{
//		if (cLastBlock!=pFreeTable[i])
//		{
//			// change 
//			if (1==cLastBlock)
//			{
//				// we have the first empty space
//				*pStart = i * (u64)(0x8000);
//				*pSize = (u64)(0x7c00);
//			}
//			else
//			{
//				// now found the end so simply can return as we have switched from a free to a used
//				// we have to tweak the start though as we need to remove a few values
//				return;
//			}
//		}
//		else
//		{
//			// same so if we have a potential run
//			if (0==cLastBlock)
//			{
//				// add the block to the size
//				*pSize += (u64)(0x7c00);
//			}
//		}
//		cLastBlock = pFreeTable[i];
//	}
//	
//	// if we get here then there is no free space OR we have passed the end of the
//	// image after starting
//	if (0==cLastBlock)
//	{
//		// all okay and values are correct
//	}
//	else
//	{
//		// no free space
//	}
//}

////////////////////////////////////////////////////////////
// Inverse of the be32 function - writes a 32 bit value   //
// high to low                                            //
////////////////////////////////////////////////////////////
//void CWIIDisc::Write32( u8 *p, u32 nVal)
//{
//	p[0] = (nVal >> 24) & 0xFF;
//	p[1] = (nVal >> 16) & 0xFF;
//	p[2] = (nVal >>  8) & 0xFF;
//	p[3] = (nVal      ) & 0xFF;
//
//}
////////////////////////////////////////////////////////////////////////////////////////
// This function takes two FSTs and merges them using a passed offset as the start    //
// location for the new data.                                                         //
// UNFINISHED - UNFINISHED - UNFINISHED ETC...........................................//
////////////////////////////////////////////////////////////////////////////////////////
//BOOL CWIIDisc::MergeAndRelocateFSTs(unsigned char *pFST1, u32 nSizeofFST1, unsigned char *pFST2, u32 nSizeofFST2, unsigned char *pNewFST,  u32 * nSizeofNewFST, u64 nNewOffset, u64 nOldOffset)
//{
//
//	u32 nFilesFST1 = 0;
//	u32 nFilesFST2 = 0;
//	u32 nFilesNewFST = 0;
//	u32 nStringTableOffset;
//
//	u64	nOffsetCalc = 0;
//	u32 nStringPad =   0;
//	
//	// extract the data for FST 1
//	nFilesFST1 = be32(pFST1 + 8);
//
//	// extract the data for FST 2
//	nFilesFST2 = be32(pFST1 + 8);
//
//	// merge the two entry offset tables (as we then will know where the string table starts)
//	// copy the first one over
//	memcpy(pNewFST, pFST1, nFilesFST1 * 0x0C);
//	memcpy(pNewFST + (nFilesFST1 * 0x0C), pFST2 + 0x0C, (nFilesFST2 - 1)*0x0C);
//
//	nStringTableOffset = (nFilesFST1 + nFilesFST2 -1) * 0x0c;
//
//	// now copy the string tables
//	memcpy(pNewFST + nStringTableOffset, pFST1 + (nFilesFST1 * 0x0C), nSizeofFST1 - (nFilesFST1 * 0x0C));
//
//	// now search back to find the first non 0 character
//	nStringPad = nSizeofFST1 + ((nFilesFST2 -1) * 0x0C);
//
//	while (0==pNewFST[nStringPad])
//	{
//		nStringPad --;
//	}
//	nStringPad +=2;
//
//	// so that we then know the real positional offset to write to
//
//	memcpy(pNewFST + nStringPad, pFST2 + (nFilesFST2 * 0x0C), nSizeofFST2 - (nFilesFST2 * 0x0C));
//
//	// we then need to go through both sets of data in the copied FST2 data to mark
//	// them correctly
//
//	// HOW TO HANDLE DUPLICATE FILENAMES???
//	// 
//
//
//
//	// TO BE DONE - BUT NOT IN THIS APPLICATION ;)
//
//	return TRUE;
//}

//////////////////////////////////////////////////////////////////////////////
// Invert of the mark as used - to allow for                                //
// creation of a DIF file for a specific area e.g. mariokart partition 3    //
// Not really used these days                                               //
//////////////////////////////////////////////////////////////////////////////
//void CWIIDisc::MarkAsUnused(u64 nOffset, u64 nSize)
//{
//		u64 nStartValue = nOffset;
//		u64 nEndValue = nOffset + nSize;
//		while((nStartValue < nEndValue)&&
//			(nStartValue < ((u64)(4699979776) * (u64)(2))))
//		{
//
//			pFreeTable[nStartValue / (u64)(0x8000)] = 0;
//			nStartValue = nStartValue + ((u64)(0x8000));
//		}
//
//}

//BOOL CWIIDisc::DiscWriteDirect(struct image_file *image, u64 nOffset, u8 *pData, unsigned int nSize)
//{
//
//	_int64 nSeek;
//
//	// Simply seek to the right place
//	nSeek = _lseeki64 (image->fp, nOffset, SEEK_SET);
//
//    if (-1==nSeek)
//	{
//		//m_pParent->AddToLog("Seek error for write");
//		AfxMessageBox("io_seek");
//        return FALSE;
//    }
//
//	if (nSize!= _write(image->fp, pData, nSize))
//	{
//		//m_pParent->AddToLog("Write error");
//		AfxMessageBox("Write error");
//        return FALSE;
//	}
//	return TRUE;
//}

//////////////////////////////////////////////////////////////////////////
// The infamous TRUCHA signing bug                                      //
// where we change the reserved bytes in the ticket until the SHA has a //
// 0 in the first location                                              //
//////////////////////////////////////////////////////////////////////////

//BOOL CWIIDisc::wii_trucha_signing(struct image_file *image, int partition)
//{
//	u8 *buf, hash[20];
//	u32 size, val;
//
//	/* Store TMD size */
//	size = (u32)(image->parts[partition].tmd_size);
//
//	/* Allocate memory */
//	buf = (u8 *)calloc(size,1);
//
//	if (!buf)
//	{
//		return FALSE;
//	}
//
//	/* Jump to the partition TMD and read it*/
//	_lseeki64(image->fp, image->parts[partition].offset + image->parts[partition].tmd_offset, SEEK_SET);
//	if (size!=_read(image->fp, buf, size))
//	{
//		return FALSE;
//	}
//
//	/* Overwrite signature with trucha signature */
//	memcpy(buf + 0x04, trucha_signature, 256);
//
//	/* SHA-1 brute force */
//	hash[0] = 1;
//	for (val = 0; ((val <= ULONG_MAX)&&(hash[0]!=0x00)); val++)
//	{
//		/* Modify TMD "reserved" field */
//		memcpy(buf + 0x19A, &val, sizeof(val));
//
//		/* Calculate SHA-1 hash */
//		SHA1(buf + 0x140, size - 0x140, hash);
//
//
//		// check for the bug where the first byte of the hash is 0
//		if (0x00==hash[0])
//		{
//			/* Write modified TMD data */
//			_lseeki64(image->fp, image->parts[partition].offset + image->parts[partition].tmd_offset, SEEK_SET);
//
//			// write it out
//			if (size!= _write(image->fp, buf, size))
//			{
//				// error writing
//				return  FALSE;
//			}
//			else
//			{
//				return TRUE;
//			}
//		}
//	}
//	return FALSE;
//}

// calculate the number of clusters

//int CWIIDisc::wii_nb_cluster(struct image_file *iso, int partition)
//{
//	int nRetVal = 0;
//
//	nRetVal = (int)(iso->parts[partition].data_size / SIZE_CLUSTER);
//
//	return nRetVal;
//}
//
//// calculate the group hash for a cluster
//BOOL CWIIDisc::wii_calc_group_hash(struct image_file *iso, int partition, int cluster)
//{
//	u8 h2[SIZE_H2], h3[SIZE_H3], h4[SIZE_H4];
//	u32 group;
//
//	/* Calculate cluster group */
//	group = cluster / NB_CLUSTER_GROUP;
//
//	/* Get H2 hash of the group */
//	if (FALSE==wii_read_cluster_hashes(iso, partition, cluster, NULL, NULL, h2))
//	{
//		return FALSE;
//	}
//
//	/* read the H3 table offset */
//	io_read(h3, SIZE_H3, iso, iso->parts[partition].offset + iso->parts[partition].h3_offset);
//
//
//	/* Calculate SHA-1 hash */
//	sha1(h2, SIZE_H2, &h3[group * 0x14]);
//
//	/* Write new H3 table */
//	if (FALSE==DiscWriteDirect(iso, iso->parts[partition].h3_offset + iso->parts[partition].offset, h3, SIZE_H3))
//	{
//		return FALSE;
//	}
//	
//
//	/* Calculate H4 */
//	sha1(h3, SIZE_H3, h4);
//
//	/* Write H4 */
//	if (FALSE==DiscWriteDirect(iso, iso->parts[partition].tmd_offset + OFFSET_TMD_HASH + iso->parts[partition].offset, h4, SIZE_H4))
//	{
//		return FALSE;
//	}
//
//
//	return TRUE;
//}
//
//int CWIIDisc::wii_read_cluster(struct image_file *iso, int partition, int cluster, u8 *data, u8 *header)
//{
//	u8 buf[SIZE_CLUSTER];
//	u8  iv[16];
//	u8 * title_key;
//	u64 offset;
//
//
//	/* Jump to the specified cluster and copy it to memory */
//	offset = iso->parts[partition].offset + iso->parts[partition].data_offset + (u64)((u64)cluster * (u64)SIZE_CLUSTER);
//	
//	// read the correct location block in
//	io_read(buf, SIZE_CLUSTER, iso, offset);
//
//	/* Set title key */
//	title_key =  &(iso->parts[partition].title_key[0]);
//
//	/* Copy header if required*/
//	if (header)
//	{
//		/* Set IV key to all 0's*/
//		memset(iv, 0, sizeof(iv));
//
//		/* Decrypt cluster header */
//		aes_cbc_dec(buf, header, SIZE_CLUSTER_HEADER, title_key, iv);
//	}
//
//	/* Copy data if required */
//	if (data)
//	{
//		/* Set IV key to correct location*/
//		memcpy(iv, &buf[OFFSET_CLUSTER_IV], 16);
//
//		/* Decrypt cluster data */
//		aes_cbc_dec(&buf[0x400], data, SIZE_CLUSTER_DATA, title_key,  &iv[0]);
//
//	}
//
//	return 0;
//}
//
//int CWIIDisc::wii_write_cluster(struct image_file *iso, int partition, int cluster, u8 *in)
//{
//	u8 h0[SIZE_H0];
//	u8 h1[SIZE_H1];
//	u8 h2[SIZE_H2];
//
//	u8 *data;
//	u8 *header;
//	u8 *title_key;
//
//	u8 iv[16];
//	
//	u32 group,
//		subgroup,
//		f_cluster,
//		nb_cluster,
//		pos_cluster,
//		pos_header;
//	
//	u64 offset;
//
//	u32 i;
//	int ret = 0;
//
//	/* Calculate cluster group and subgroup */
//	group = cluster / NB_CLUSTER_GROUP;
//	subgroup = (cluster % NB_CLUSTER_GROUP) / NB_CLUSTER_SUBGROUP;
//
//	/* First cluster in the group */
//	f_cluster = group * NB_CLUSTER_GROUP;
//
//	/* Get number of clusters in this group */
//	nb_cluster = wii_nb_cluster(iso, partition) - f_cluster;
//	if (nb_cluster > NB_CLUSTER_GROUP)
//		nb_cluster = NB_CLUSTER_GROUP;
//
//	/* Allocate memory */
//	data   = (u8 *)calloc(SIZE_CLUSTER_DATA * NB_CLUSTER_GROUP, 1);
//	header = (u8 *)calloc(SIZE_CLUSTER_HEADER * NB_CLUSTER_GROUP, 1);
//	if (!data || !header)
//		return 1;
//
//	/* Read group clusters and headers */
//	for (i = 0; i < nb_cluster; i++)
//	{
//		u8 *d_ptr = &data[SIZE_CLUSTER_DATA * i];
//		u8 *h_ptr = &header[SIZE_CLUSTER_HEADER * i];
//
//		/* Read cluster */
//		if (wii_read_cluster(iso, partition, f_cluster + i, d_ptr, h_ptr))
//		{
//			free(data);
//			free(header);
//			return FALSE;
//		}
//	}
//
//	/* Calculate new cluster H0 table */
//	for (i = 0; i < SIZE_CLUSTER_DATA; i += 0x400)
//	{
//		u32 idx = (i / 0x400) * 20;
//
//		/* Calculate SHA-1 hash */
//		sha1(&in[i], 0x400, &h0[idx]);
//	}
//
//	/* Write new cluster and H0 table */
//	pos_header  = ((cluster - f_cluster) * SIZE_CLUSTER_HEADER);
//	pos_cluster = ((cluster - f_cluster) * SIZE_CLUSTER_DATA);
//
//	memcpy(&data[pos_cluster], in, SIZE_CLUSTER_DATA);
//	memcpy(&header[pos_header + OFFSET_H0], h0, SIZE_H0);
//
//	/* Calculate H1 */
//	for (i = 0; i < NB_CLUSTER_SUBGROUP; i++)
//	{
//		u32 pos = SIZE_CLUSTER_HEADER * ((subgroup * NB_CLUSTER_SUBGROUP) + i);
//		u8 tmp[SIZE_H0];
//
//		/* Cluster exists? */
//		if ((pos / SIZE_CLUSTER_HEADER) > nb_cluster)
//			break;
//
//		/* Get H0 */
//		memcpy(tmp, &header[pos + OFFSET_H0], SIZE_H0);
//
//		/* Calculate SHA-1 hash */
//		sha1(tmp, SIZE_H0, &h1[20 * i]);
//	}
//
//	/* Write H1 table */
//	for (i = 0; i < NB_CLUSTER_SUBGROUP; i++)
//	{
//		u32 pos = SIZE_CLUSTER_HEADER * ((subgroup * NB_CLUSTER_SUBGROUP) + i);
//
//		/* Cluster exists? */
//		if ((pos / SIZE_CLUSTER_HEADER) > nb_cluster)
//			break;
//
//		/* Write H1 table */
//		memcpy(&header[pos + OFFSET_H1], h1, SIZE_H1);
//	}
//
//	/* Calculate H2 */
//	for (i = 0; i < NB_CLUSTER_SUBGROUP; i++)
//	{
//		u32 pos = (NB_CLUSTER_SUBGROUP * i) * SIZE_CLUSTER_HEADER;
//		u8 tmp[SIZE_H1];
//
//		/* Cluster exists? */
//		if ((pos / SIZE_CLUSTER_HEADER) > nb_cluster)
//			break;
//
//		/* Get H1 */
//		memcpy(tmp, &header[pos + OFFSET_H1], SIZE_H1);
//
//		/* Calculate SHA-1 hash */
//		sha1(tmp, SIZE_H1, &h2[20 * i]);
//	}
//
//	/* Write H2 table */
//	for (i = 0; i < nb_cluster; i++)
//	{
//		u32 nb = SIZE_CLUSTER_HEADER * i;
//
//		/* Write H2 table */
//		memcpy(&header[nb + OFFSET_H2], h2, SIZE_H2);
//	}
//
//	/* Set title key */
//	title_key = &(iso->parts[partition].title_key[0]);
//
//	/* Encrypt headers */
//	for (i = 0; i < nb_cluster; i++)
//	{
//		u8 *ptr = &header[SIZE_CLUSTER_HEADER * i];
//
//		u8 phData[SIZE_CLUSTER_HEADER];
//
//		/* Set IV key */
//		memset(iv, 0, 16);
//
//		/* Encrypt */
//		aes_cbc_enc(ptr, (u8*) phData, SIZE_CLUSTER_HEADER, title_key, iv);
//		memcpy(ptr, (u8*)phData, SIZE_CLUSTER_HEADER);
//	}
//
//	/* Encrypt clusters */
//	for (i = 0; i < nb_cluster; i++)
//	{
//		u8 *d_ptr = &data[SIZE_CLUSTER_DATA * i];
//		u8 *h_ptr = &header[SIZE_CLUSTER_HEADER * i];
//
//		u8 phData[SIZE_CLUSTER_DATA];
//
//
//		/* Set IV key */
//		memcpy(iv, &h_ptr[OFFSET_CLUSTER_IV], 16);
//
//		/* Encrypt */
//		aes_cbc_enc(d_ptr, (u8*) phData, SIZE_CLUSTER_DATA, title_key, iv);
//		memcpy(d_ptr, (u8*)phData, SIZE_CLUSTER_DATA);
//	}
//
//	/* Jump to first cluster in the group */
//	offset = iso->parts[partition].offset + iso->parts[partition].data_offset + (u64)((u64)f_cluster * (u64)SIZE_CLUSTER);
//
//	/* Write new clusters */
//	for (i = 0; i < nb_cluster; i++)
//	{
//		u8 *d_ptr = &data[SIZE_CLUSTER_DATA * i];
//		u8 *h_ptr = &header[SIZE_CLUSTER_HEADER * i];
//
//		/* Write cluster header */
//		if (TRUE==DiscWriteDirect(iso, offset, h_ptr, SIZE_CLUSTER_HEADER))
//		{
//			// written ok, add value to offset
//			offset = offset + SIZE_CLUSTER_HEADER;
//
//			if (TRUE==DiscWriteDirect(iso, offset, d_ptr, SIZE_CLUSTER_DATA))
//			{
//				offset = offset + SIZE_CLUSTER_DATA;
//			}
//			else
//			{
//				free(data);
//				free(header);
//				return FALSE;
//
//			}
//		}
//		else
//		{
//			// free memory and return error
//			free(data);
//			free(header);
//			return FALSE;
//		}
//	}
//
//	/* Recalculate global hash table */
//	if (wii_calc_group_hash(iso, partition, cluster))
//	{
//		free(data);
//		free(header);
//		return FALSE;
//	}
//
//	/* Free memory */
//	free(data);
//	free(header);
//
//	return TRUE;
//}
//
//
//BOOL CWIIDisc::wii_read_cluster_hashes(struct image_file *iso, int partition, int cluster, u8 *h0, u8 *h1, u8 *h2)
//{
//	u8 buf[SIZE_CLUSTER_HEADER];
//
//	/* Read cluster header */
//	if (wii_read_cluster(iso, partition, cluster, NULL, buf))
//		return FALSE;
//
//	if (NULL!=h0)
//		memcpy(h0, buf + OFFSET_H0, SIZE_H0);
//	if (NULL!=h1)
//		memcpy(h1, buf + OFFSET_H1, SIZE_H1);
//	if (NULL!=h2)
//		memcpy(h2, buf + OFFSET_H2, SIZE_H2);
//
//	return TRUE;
//}
//
//int CWIIDisc::wii_read_data(struct image_file *iso, int partition, u64 offset, u32 size, u8 **out)
//{
//	u8 *buf, *tmp;
//	u32 cluster_start, clusters, i, offset_start;
//
//
//	/* Calculate some needed information */
//	cluster_start = (u32)(offset / (u64)(SIZE_CLUSTER_DATA));
//	clusters = (u32)(((offset + (u64)(size)) / (u64)(SIZE_CLUSTER_DATA))) - (cluster_start - 1);
//	offset_start = (u32)(offset - (cluster_start * (u64)(SIZE_CLUSTER_DATA)));
//
//	/* Allocate memory */
//	buf = (u8 *)calloc(clusters * SIZE_CLUSTER_DATA,1);
//	if (!buf)
//		return 1;
//
//	/* Read clusters */
//	for (i = 0; i < clusters; i++)
//		if (wii_read_cluster(iso, partition, cluster_start + i, &buf[SIZE_CLUSTER_DATA * i], NULL))
//			return 1;
//
//	/* Allocate memory */
//	tmp = (u8 *)calloc(size,1);
//	if (!tmp)
//		return 1;
//
//	/* Copy specified data */
//	memcpy(tmp, buf + offset_start, size);
//
//	/* Free unused memory */
//	free(buf);
//
//	/* Set pointer address */
//	*out = tmp;
//
//	return 0;
//}
//
//
//void CWIIDisc::sha1(u8 *data, u32 len, u8 *hash)
//{
//	SHA1(data, len, hash);
//}
//
//void CWIIDisc::aes_cbc_enc(u8 *in, u8 *out, u32 len, u8 *key, u8 *iv)
//{
//	AES_KEY aes_key;
//
//	/* Set encryption key */
//	AES_set_encrypt_key(key, 128, &aes_key);
//
//	/* Decrypt data */
//	AES_cbc_encrypt(in, out, len, &aes_key, iv, AES_ENCRYPT);
//}
//
//void CWIIDisc::aes_cbc_dec(u8 *in, u8 *out, u32 len, u8 *key, u8 *iv)
//{
//	AES_KEY aes_key;
//
//	/* Set decryption key */
//	AES_set_decrypt_key(key, 128, &aes_key);
//
//	/* Decrypt data */
//	AES_cbc_encrypt(in, out, len, &aes_key, iv, AES_DECRYPT);
//}
//
//u64 CWIIDisc::FindRequiredFreeSpaceInPartition(struct image_file *image, u64 nPartition, u32 nRequiredSize)
//{
//
//	// search through the free space to find a free area that is at least
//	// the required size. We can then return the position of the free space
//	// relative to the data area of the partition
//	char cLastBlock = 1; // assume (!) that we have data at the partition start
//
//	u32 nRequiredBlocks = (nRequiredSize / 0x7c00);
//
//	if (0!=(nRequiredSize%0x7c00))
//	{
//		// we require an extra block
//		nRequiredBlocks++;
//	}
//
//	u64 nReturnOffset = 0;
//
//	u64	nStartOffset = image->parts[nPartition].offset + image->parts[nPartition].data_offset;
//
//	u64 nEndOffset = nStartOffset + image->parts[nPartition].data_size;
//	u64 nCurrentOffset = nStartOffset;
//
//	u64	nMarkedStart = 0;
//	u32 nFreeBlocks = 0;
//	u32 nBlock = 0;
//
//	// now go through the marked list to find the free area of the required size
//	while (nCurrentOffset < nEndOffset)
//	{
//		nBlock = (u32)(nCurrentOffset / (u64)(0x8000));
//		if (cLastBlock!=pFreeTable[nBlock])
//		{
//			// change 
//			if (1==cLastBlock)
//			{
//				// we have the first empty space
//				nMarkedStart = nCurrentOffset;
//				nFreeBlocks = 1;
//			}
//			// else we just store the value and wait for one of the other fallouts
//		}
//		else
//		{
//			// same so if we have a potential run
//			if (0==cLastBlock)
//			{
//				// add the block to the size
//				nFreeBlocks ++;
//				// now check to see if we have enough
//				if (nFreeBlocks >= nRequiredBlocks)
//				{
//					// BINGO! - convert into relative offset from data start
//					// and in encrypted format
//					nReturnOffset = ((nMarkedStart - nStartOffset) / (u64) 0x8000) * (u64)(0x7c00);
//					return nReturnOffset;
//				}
//			}
//		}
//		cLastBlock = pFreeTable[nBlock];
//
//		nCurrentOffset = nCurrentOffset + (u64)(0x8000);
//	}
//
//	// if we get here then we didn't find some space :(
//
//	return 0;
//}
//
///////////////////////////////////////////////////////////////
//// Check to see if we have free space for so many blocks   //
///////////////////////////////////////////////////////////////
//
//BOOL CWIIDisc::CheckForFreeSpace(image_file *image, u32 nPartition, u64 nOffset, u32 nBlocks)
//{
//
//	// convert offset to block representation
//	u32 nBlockOffsetStart = 0;
//
//	nBlockOffsetStart = (u32)((image->parts[nPartition].data_offset + image->parts[nPartition].offset) / (u64)0x8000);
//	nBlockOffsetStart = nBlockOffsetStart + (u32)(nOffset / (u64) 0x7c00);
//	if (0!=nOffset%0x7c00)
//	{
//		// starts part way into a block so need to check the number of blocks plus one
//		nBlocks++;
//		// and the start is up by one as we know that there must be data in the current
//		// block
//		nBlockOffsetStart++;
//	}
//
//	for (u32 x = 0; x < nBlocks; x++)
//	{
//		if (1==pFreeTable[nBlockOffsetStart+x])
//		{
//			return FALSE;
//		}
//	}
//	return TRUE;
//}
//
////////////////////////////////////////////////////////////////////////////
//// Routine deletes the highlighted partition                            //
//// does this by moving all the sucessive data up in the partition table //
//// to overwrite the deleted partition                                   //
//// It then updates the partition count                                  //
//// Also works on channels                                               //
////////////////////////////////////////////////////////////////////////////
//BOOL CWIIDisc::DeletePartition(image_file *image, u32 nPartition)
//{
//
//	u8	buffer[16];
//	u64	WriteLocation;
//	int	i;
//
//	memset(buffer,0,16);
//
//	// check the partition is either a partition or a channel
//	if (PART_VC == image->parts[nPartition].type)
//	{
//		// use the channels
//		
//		// find out which partition we are really deleting
//		// as the value is offset by the number of real partitions
//		nPartition = nPartition - image->PartitionCount;
//
//		// update the count of channels in the correct location
//		Write32(buffer, image->ChannelCount -1);
//		DiscWriteDirect(image, (u64) 0x40008, buffer, 4);
//
//		// create the updated channel list in the correct location on the disc 
//		WriteLocation = image->chan_tbl_offset + (u64)(8)*(u64)(nPartition - 1);
//
//		for (i = nPartition; i < image->ChannelCount; i++)
//		{
//			// read the next partition info
//			io_read(buffer, 8, image, image->chan_tbl_offset + (u64)(8)*(u64)(i));
//			// write it out over the deleted one
//			DiscWriteDirect(image, WriteLocation, buffer, 8);
//			WriteLocation = WriteLocation + 8;
//		}
//		// now overwrite the last one with blanks
//		memset(buffer,0,16);
//		DiscWriteDirect(image, WriteLocation, buffer, 8);
//
//	}
//	else
//	{
//		// it's the partition table
//
//		// update the count of partitions
//		Write32(buffer, image->PartitionCount -1);
//		DiscWriteDirect(image, (u64) 0x40000, buffer, 4);
//		
//		// create the partition table
//		WriteLocation = image->part_tbl_offset + (u64)(8)*(u64)(nPartition - 1);
//
//		for (i = nPartition; i < image->PartitionCount; i++)
//		{
//			// read the next partition info
//			io_read(buffer, 8, image, image->part_tbl_offset + (u64)(8)*(u64)(i));
//			// write it out over the deleted one
//			DiscWriteDirect(image, WriteLocation, buffer, 8);
//			WriteLocation = WriteLocation + 8;
//		}
//		// now overwrite the last one with blanks
//		memset(buffer,0,16);
//		DiscWriteDirect(image, WriteLocation, buffer, 8);
//
//	}
//
//	return TRUE;
//}
//
////////////////////////////////////////////////////////////////////////////
//// Resize the partition data size field                                 //
//// as some discs have 'interesting' values in here                      //
////////////////////////////////////////////////////////////////////////////
////BOOL CWIIDisc::ResizePartition(image_file *image, u32 nPartition)
////{
//	//u64 nCurrentSize = 0;
//	//u64 nMinimumSize = 0;
//	//u64 nMaximumSize = 0;
//	//u64 nNewSize = 0;
//
//	//u8 buffer[16];
//
//	//// Get size of current partition
//	//nCurrentSize = image->parts[nPartition].data_size;
//	//nNewSize = nCurrentSize;
//
//	//// calculate maximum size (based on next partition start)
//	//// or disc size if the last one
//	//
//	//if ((nPartition+1)==image->nparts)
//	//{
//	//	nMaximumSize = nImageSize;
//	//}
//	//else
//	//{
//	//	nMaximumSize = image->parts[nPartition+1].offset;
//	//}
//	//nMaximumSize = nMaximumSize - image->parts[nPartition].offset;
//	//nMaximumSize = nMaximumSize - image->parts[nPartition].data_offset;
//
//	//// calculate minimum size by looking for where the data is
//	//// on the disc backwards from the current partition data end
//	//// create the window with the data
//	//
//	//nMinimumSize = SearchBackwards(nMaximumSize, image->parts[nPartition].offset + image->parts[nPartition].data_offset);
//	//
//	//// create window and 
//	//// and then ask for the values
//	////CResizePartition * pWindow = new CResizePartition();
//
//
//	////pWindow->SetRanges(nMinimumSize, nCurrentSize, nMaximumSize);
//
//	//if (IDOK==pWindow->DoModal())
//	//{
//	//	// if values changed and OK pressed then update the correct pointer in the disc
//	//	// image
//	//	nNewSize = pWindow->GetNewSize();
//
//	//	delete pWindow;
//	//	if (nNewSize == nCurrentSize)
//	//	{
//	//		AddToLog("Sizes the same");
//	//		return FALSE;
//	//	}
//
//	//	// now simply write out the new size and store it
//	//	image->parts[nPartition].data_size = nNewSize;
//	//	Write32(buffer, (u32)((u64) nNewSize >> 2));
//	//	DiscWriteDirect(image, image->parts[nPartition].offset + 0x2bc, buffer, 4);
//
//	//	return TRUE;
//	//}
//	//// don't even need to reparse as the values will be updated internally
//	//delete pWindow;
//	//return FALSE;
////}
//
//
//u64 CWIIDisc::SearchBackwards(u64 nStartPosition, u64 nEndPosition)
//{
//
//	u64 nCurrentBlock;
//	u64 nEndBlock;
//	u64	nStartBlock;
//
//	nCurrentBlock = (nStartPosition + nEndPosition - 1)/ (u64)(0x8000);
//	nStartBlock = nCurrentBlock;
//
//	nEndBlock = nEndPosition / (u64)(0x8000);
//
//	while (nCurrentBlock > nEndBlock)
//	{
//		if (0==pFreeTable[nCurrentBlock])
//		{
//			nCurrentBlock --;
//		}
//		else
//		{
//			// if it's the first block then we are at the start position
//			if (nStartBlock==nCurrentBlock)
//			{
//				return (nCurrentBlock - nEndBlock + 1)* ((u64)(0x8000));
//			}
//			else
//			{
//				return (nCurrentBlock - nEndBlock )* ((u64)(0x8000));
//			}
//		}
//	}
//	return 0;
//}
//
//
//////////////////////////////////////////////////////////////////////////////
//// Modification of the write_cluster function to write multiple clusters  //
//// in one sitting. This means the disc access should then be minimized    //
//// It also allows for a file to be used for the input instead of a memory //
//// pointer as that allows for larger files to be updated. I'm talking to  //
//// you Okami.....                                                         //
//////////////////////////////////////////////////////////////////////////////
//BOOL CWIIDisc::wii_write_clusters(struct image_file *iso, int partition, int cluster, u8 *in, u32 nClusterOffset, u32 nBytesToWrite, FILE * fIn)
//{
//	u8 h0[SIZE_H0];
//	u8 h1[SIZE_H1];
//	u8 h2[SIZE_H2];
//
//	u8 *data;
//	u8 *header;
//	u8 *title_key;
//
//	u8 iv[16];
//	
//	u32 group,
//		subgroup,
//		f_cluster,
//		nb_cluster,
//		pos_cluster,
//		pos_header;
//	
//	u64 offset;
//
//	u32 i;
//	int j;
//
//	int ret = 0;
//
//	int nClusters = 0;
//
//	/* Calculate cluster group and subgroup */
//	group = cluster / NB_CLUSTER_GROUP;
//	subgroup = (cluster % NB_CLUSTER_GROUP) / NB_CLUSTER_SUBGROUP;
//
//	/* First cluster in the group */
//	f_cluster = group * NB_CLUSTER_GROUP;
//
//	/* Get number of clusters in this group */
//	nb_cluster = wii_nb_cluster(iso, partition) - f_cluster;
//	if (nb_cluster > NB_CLUSTER_GROUP)
//		nb_cluster = NB_CLUSTER_GROUP;
//
//	/* Allocate memory */
//	data   = (u8 *)calloc(SIZE_CLUSTER_DATA * NB_CLUSTER_GROUP, 1);
//	header = (u8 *)calloc(SIZE_CLUSTER_HEADER * NB_CLUSTER_GROUP, 1);
//	if (!data || !header)
//		return FALSE;
//
//	// if we are replacing a full set of clusters then we don't
//	// need to do any reading as we just need to overwrite the
//	// blanked data
//
//
//	// calculate number of clusters of data to write
//	nClusters = ((nBytesToWrite -1)/ SIZE_CLUSTER_DATA)+1;
//
//	if (nBytesToWrite!=(NB_CLUSTER_GROUP*SIZE_CLUSTER_DATA))
//	{
//		/* Read group clusters and headers */
//		for (i = 0; i < nb_cluster; i++)
//		{
//			u8 *d_ptr = &data[SIZE_CLUSTER_DATA * i];
//			u8 *h_ptr = &header[SIZE_CLUSTER_HEADER * i];
//			
//			/* Read cluster */
//			if (wii_read_cluster(iso, partition, f_cluster + i, d_ptr, h_ptr))
//			{
//				free(data);
//				free(header);
//				return FALSE;
//			}
//		}
//	}
//	else
//	{
//		// memory already cleared
//	}
//
//	// now overwrite the data in the correct location
//	// be it from file data or from the memory location
//	/* Write new cluster and H0 table */
//	pos_header  = ((cluster - f_cluster) * SIZE_CLUSTER_HEADER);
//	pos_cluster = ((cluster - f_cluster) * SIZE_CLUSTER_DATA);
//
//
//	// we read from either memory or a file
//	if (NULL!=fIn)
//	{
//		fread(&data[pos_cluster + nClusterOffset],1, nBytesToWrite, fIn); 
//	}
//	else
//	{
//		// data
//		memcpy(&data[pos_cluster + nClusterOffset], in, nBytesToWrite);
//	}
//
//	// now for each cluster we need to...
//	for(j = 0; j < nClusters; j++)
//	{
//		// clear the data for the table
//		memset(h0, 0, SIZE_H0);
//
//		/* Calculate new clusters H0 table */
//		for (i = 0; i < SIZE_CLUSTER_DATA; i += 0x400)
//		{
//			u32 idx = (i / 0x400) * 20;
//			
//			/* Calculate SHA-1 hash */
//			sha1(&data[pos_cluster + (j * SIZE_CLUSTER_DATA) + i], 0x400, &h0[idx]);
//		}
//		
//		// save the H0 data
//		memcpy(&header[pos_header + (j * SIZE_CLUSTER_HEADER)], h0, SIZE_H0);
//
//		// now do the H1 data for the subgroup
//		/* Calculate H1's */
//		sha1(&header[pos_header + (j * SIZE_CLUSTER_HEADER)], SIZE_H0, h1);
//
//		// now copy to all the sub cluster locations
//		for (int k=0; k < NB_CLUSTER_SUBGROUP; k++)
//		{
//			// need to get the position of the first block we are changing
//			// which is the start of the subgroup for the current cluster 
//			u32 nSubGroup = ((cluster + j) % NB_CLUSTER_GROUP) / NB_CLUSTER_SUBGROUP;
//
//			u32 pos = (SIZE_CLUSTER_HEADER * nSubGroup * NB_CLUSTER_SUBGROUP) + (0x14 * ((cluster +j)%NB_CLUSTER_SUBGROUP));
//
//			memcpy(&header[pos + (k * SIZE_CLUSTER_HEADER) + OFFSET_H1], h1, 20);
//		}
//
//	}
//
//
//	// now we need to calculate the H2's for all subgroups
//	/* Calculate H2 */
//	for (i = 0; i < NB_CLUSTER_SUBGROUP; i++)
//	{
//		u32 pos = (NB_CLUSTER_SUBGROUP * i) * SIZE_CLUSTER_HEADER;
//		
//		/* Cluster exists? */
//		if ((pos / SIZE_CLUSTER_HEADER) > nb_cluster)
//			break;
//		
//		/* Calculate SHA-1 hash */
//		sha1(&header[pos + OFFSET_H1], SIZE_H1, &h2[20 * i]);
//	}
//	
//	/* Write H2 table */
//	for (i = 0; i < nb_cluster; i++)
//	{
//		/* Write H2 table */
//		memcpy(&header[(SIZE_CLUSTER_HEADER * i) + OFFSET_H2], h2, SIZE_H2);
//	}
//
//	// update the H3 key table here
//	/* Calculate SHA-1 hash */
//	sha1(h2, SIZE_H2, &h3[group * 0x14]);
//
//
//	// now encrypt and write
//	
//	/* Set title key */
//	title_key = &(iso->parts[partition].title_key[0]);
//
//	/* Encrypt headers */
//	for (i = 0; i < nb_cluster; i++)
//	{
//		u8 *ptr = &header[SIZE_CLUSTER_HEADER * i];
//
//		u8 phData[SIZE_CLUSTER_HEADER];
//
//		/* Set IV key */
//		memset(iv, 0, 16);
//
//		/* Encrypt */
//		aes_cbc_enc(ptr, (u8*) phData, SIZE_CLUSTER_HEADER, title_key, iv);
//		memcpy(ptr, (u8*)phData, SIZE_CLUSTER_HEADER);
//	}
//
//	/* Encrypt clusters */
//	for (i = 0; i < nb_cluster; i++)
//	{
//		u8 *d_ptr = &data[SIZE_CLUSTER_DATA * i];
//		u8 *h_ptr = &header[SIZE_CLUSTER_HEADER * i];
//
//		u8 phData[SIZE_CLUSTER_DATA];
//
//
//		/* Set IV key */
//		memcpy(iv, &h_ptr[OFFSET_CLUSTER_IV], 16);
//
//		/* Encrypt */
//		aes_cbc_enc(d_ptr, (u8*) phData, SIZE_CLUSTER_DATA, title_key, iv);
//		memcpy(d_ptr, (u8*)phData, SIZE_CLUSTER_DATA);
//	}
//
//	/* Jump to first cluster in the group */
//	offset = iso->parts[partition].offset + iso->parts[partition].data_offset + (u64)((u64)f_cluster * (u64)SIZE_CLUSTER);
//
//	for (i = 0; i < nb_cluster; i++)
//	{
//		u8 *d_ptr = &data[SIZE_CLUSTER_DATA * i];
//		u8 *h_ptr = &header[SIZE_CLUSTER_HEADER * i];
//
//		if (TRUE==DiscWriteDirect(iso, offset, h_ptr, SIZE_CLUSTER_HEADER))
//		{
//			// written ok, add value to offset
//			offset = offset + SIZE_CLUSTER_HEADER;
//
//			if (TRUE==DiscWriteDirect(iso, offset, d_ptr, SIZE_CLUSTER_DATA))
//			{
//				offset = offset + SIZE_CLUSTER_DATA;
//			}
//			else
//			{
//				free(data);
//				free(header);
//				return FALSE;
//
//			}
//		}
//		else
//		{
//			// free memory and return error
//			free(data);
//			free(header);
//			return FALSE;
//		}
//	}
//
//
//	// already calculated the H3 and H4 hashes - rely on surrounding code to
//	// read and write those out
//
//	/* Free memory */
//	free(data);
//	free(header);
//
//	return TRUE;
//}
//
//////////////////////////////////////////////////////////////////////////////
//// Heavily optimised file write routine so that the minimum number of     //
//// SHA calculations have to be performed                                  //
//// We do this by writing in 1 clustergroup per write and calculate the    //
//// Offset to write the data in the minimum number of chunks               //
//// A bit like lumpy chunk packer from the Atari days......................//
//////////////////////////////////////////////////////////////////////////////
////BOOL CWIIDisc::wii_write_data_file(struct image_file *iso, int partition, u64 offset, u64 size, u8 *in, FILE * fIn)
////{
////	u32 cluster_start, clusters, offset_start;
////
////	u64 i;
////
////	u32 nClusterCount;
////	u32 nWritten = 0;
////    MSG msg;
////
////
////	/* Calculate some needed information */
////	cluster_start = (u32)(offset / (u64)(SIZE_CLUSTER_DATA));
////	clusters = (u32)(((offset + size) / (u64)(SIZE_CLUSTER_DATA)) - (cluster_start - 1));
////	offset_start = (u32)(offset - (cluster_start * (u64)(SIZE_CLUSTER_DATA)));
////
////
////	// read the H3 and H4
////	io_read(h3, SIZE_H3, iso, iso->parts[partition].offset + iso->parts[partition].h3_offset);
////
////	/* Write clusters */
////	i = 0;
////	nClusterCount = 0;
////
////	//CProgressBox * pProgressBox;
////	//
////	//pProgressBox = new CProgressBox(m_pParent); 
////	//
////	//pProgressBox->Create(IDD_PROGRESSBOX);
////	//
////	//pProgressBox->ShowWindow(SW_SHOW);
////	//pProgressBox->SetRange(0, clusters);
////	//
////	//pProgressBox->SetPosition(0);
////	
////
////	//pProgressBox->SetWindowMessage("Replacing file: please wait");
////	while( i < size)
////	{
////		//pProgressBox->SetPosition(nClusterCount);
////		
////		// do the message pump thang
////		
////        if (PeekMessage(&msg,
////            NULL,
////            0,
////            0,
////            PM_REMOVE))
////        {
////            // PeekMessage has found a message--process it 
////            if (msg.message != WM_CANCELLED)
////            {
////                TranslateMessage(&msg); // Translate virt. key codes 
////                DispatchMessage(&msg);  // Dispatch msg. to window 
////            }
////			else
////			{
////				// quit message received - simply exit
////				//delete pProgressBox;
////				AddToLog("Load cancelled - disc probably unusable");
////				AfxMessageBox("Load cancelled - disc probably unusable in current state");
////				return FALSE;
////			}
////        }
////
////		// now the fun bit as we need to cater for the start position changing as well as the 
////		// wrap over 
////		if ((0!=((cluster_start+nClusterCount)%64))||
////			(0!=offset_start))
////		{
////			// not at the start so our max size is different
////			// and also our cluster offset
////			nWritten = (NB_CLUSTER_GROUP - (cluster_start%64))* SIZE_CLUSTER_DATA;
////			nWritten = nWritten - offset_start;
////
////			// max check
////			if (nWritten > size)
////			{
////				nWritten = (u32)size;
////			}
////
////			if (FALSE==wii_write_clusters(iso, partition, cluster_start, in, offset_start, nWritten, fIn))
////			{
////				// Error
////				//delete pProgressBox;
////				AfxMessageBox("Error writing clusters");
////				return FALSE;
////			}
////			// round up the cluster count
////			nClusterCount = NB_CLUSTER_GROUP - (cluster_start % NB_CLUSTER_GROUP);
////		}
////		else
////		{
////			// potentially full block
////			nWritten = NB_CLUSTER_GROUP * SIZE_CLUSTER_DATA;
////
////			// max check
////			if (nWritten > (size-i))
////			{
////				nWritten = (u32)(size-i);
////			}
////
////			if (FALSE==wii_write_clusters(iso, partition, cluster_start + nClusterCount, in, offset_start, nWritten, fIn))
////			{
////				// Error
////				//delete pProgressBox;
////				AfxMessageBox("Error writing clusters");
////				return FALSE;
////			}
////			// we simply add a full cluster block
////			nClusterCount = nClusterCount + NB_CLUSTER_GROUP;
////
////		}
////		offset_start = 0;
////		i += nWritten;
////
////
////	}
////
////	//delete pProgressBox;
////
////	// write out H3 and H4
////
////	if (FALSE==DiscWriteDirect(iso, iso->parts[partition].h3_offset + iso->parts[partition].offset, h3, SIZE_H3))
////	{
////		AfxMessageBox("Unable to write H3 table");
////		return FALSE;
////	}
////	
////
////	/* Calculate H4 */
////	sha1(h3, SIZE_H3, h4);
////
////	/* Write H4 */
////	if (FALSE==DiscWriteDirect(iso, iso->parts[partition].tmd_offset + OFFSET_TMD_HASH + iso->parts[partition].offset, h4, SIZE_H4))
////	{
////		AfxMessageBox("Unable to write H4 value");
////		return FALSE;
////	}
////
////	// sign it
////	wii_trucha_signing(iso, partition);
////
////	return TRUE;
////}
//
//
////BOOL CWIIDisc::SetBootMode(image_file *image)
////{
//	//u8 cOldValue;
//	//int i;
//
//	//unsigned char cModes[5] = {'R','_','H','0','4'};
//	//CString csText;
//
//	//// get the current first byte of data from the passed ISO
//	//io_read(&cOldValue, 1, image, 0);
//
//	//for (i = 0; i < 5; i++)
//	//{
//	//	if (cOldValue==cModes[i])
//	//	{
//	//		break;
//	//	}
//	//}
//
//	//// check for error - not found
//	//// should NEVER get error as it would have failed the initial parse
//	//// routine
//	//if (5==i)
//	//{
//	//	csText.Format("Current mode not valid = %x [%c]", cOldValue, cOldValue);
//	//	AfxMessageBox(csText);
//	//	return FALSE;
//	//}
//
//	//// Create the change display
//	//// create window and 
//	//// and then ask for the values
//
//	//CBootMode * pWindow = new CBootMode();
//	//pWindow->SetBootMode(i);
//
//	//if (IDOK==pWindow->DoModal())
//	//{
//	//	// if values changed and OK pressed then update the correct pointer in the disc
//	//	// image
//	//	if (i!=pWindow->GetBootMode())
//	//		
//	//	{
//	//		// changed so alter byte
//	//		DiscWriteDirect(image, 0, &cModes[pWindow->GetBootMode()], 1);
//	//		csText.Format("Boot mode now [%c]", cModes[pWindow->GetBootMode()]);
//	//		AddToLog(csText);
//	//	}
//	//	else
//	//	{
//	//		// same value
//	//		AddToLog("Same boot mode - no action taken");
//	//	}
//
//	//	
//	//}
//	//else
//	//{
//	//	AddToLog("Boot change cancelled");
//	//}
//	//delete pWindow;
//	//return TRUE;
////}
//
//BOOL CWIIDisc::AddPartition(image_file *image, BOOL bChannel, u64 nOffset, u64 nDataSize, u8 * pText)
//{
//
//	// just try and see if this works at the moment
//	u8	buffer[16];
//	u64	WriteLocation;
//
//	memset(buffer,0,16);
//
//	// check the partition is either a partition or a channel
//	if (TRUE==bChannel)
//	{
//		// use the channels
//		// update the count of channels in the correct location
//		Write32(buffer, image->ChannelCount +1);
//		DiscWriteDirect(image, (u64) 0x40008, buffer, 4);
//
//		// check to see if we actually have any channels defined and hence a value in the channel table offset
//		if (0==image->chan_tbl_offset)
//		{
//			// we need to create the table from scratch
//			image->chan_tbl_offset = 0x41000;
//			Write32(buffer, 0x41000 >> 2);
//			DiscWriteDirect(image, (u64) 0x4000C, buffer, 4);
//		}
//		// create the updated channel list in the correct location on the disc 
//		WriteLocation = image->chan_tbl_offset + (u64)(8)*(u64)(image->ChannelCount);
//		// write out the correct data block
//		// set the buffer for start location and channel name
//		Write32(buffer, (u32)(nOffset>>2));
//		buffer[4] = pText[0];
//		buffer[5] = pText[1];
//		buffer[6] = pText[2];
//		buffer[7] = pText[3];
//
//		DiscWriteDirect(image, WriteLocation, buffer, 8);
//	
//	}
//	else
//	{
//		// it's the partition table
//
//		// update the count of partitions
//		Write32(buffer, image->PartitionCount +1);
//		DiscWriteDirect(image, (u64) 0x40000, buffer, 4);
//		
//		// create the partition table entry
//		WriteLocation = image->part_tbl_offset + (u64)(8)*(u64)(image->PartitionCount);
//
//		// set the buffer
//		Write32(buffer, (u32)(nOffset>>2));
//		Write32(buffer+4, 0);
//		
//		DiscWriteDirect(image, WriteLocation, buffer, 8);
//
//	}
//
//	// now create the necessary fake entries for all the data block values
//	// h3 = 0x2b4
//	Write32(buffer, 0x8000 >> 2);
//	// data offset = 0x2b8
//	Write32(buffer+4, 0x20000 >> 2);
//	// data size = 0x2bc
//	Write32(buffer+8, (u32)(nDataSize >> 2));
//	DiscWriteDirect(image, nOffset+0x2b4, buffer, 12);
//
//	// Should now create a fake boot.bin etc. to avoid disc reads and allow you to modify the
//	// partition
//
//
//
//	return TRUE;
//}
//
///////////////////////////////////////////////////////////////
//// Function to find out the maximum size of a partition    //
//// that can be added to the current image                  //
///////////////////////////////////////////////////////////////
//u64 CWIIDisc::GetFreeSpaceAtEnd(image_file *image)
//{
//	u64 nRetVal;
//
//	// simple enough calculation in that we simply take the last partitions
//	// offset and size and 0x1800 off the image size
//
//	if (1==image->nparts)
//	{
//		// no partitions here. We now use the image size minus the
//		// default offset of 0x50000
//
//		nRetVal = nImageSize - 0x50000;
//	}
//	else
//	{
//		// it's equal to the image minus size of the last partition and offset
//		nRetVal = nImageSize - image->parts[image->nparts-1].offset - image->parts[image->nparts-1].data_offset
//			      - image->parts[image->nparts-1].data_size;
//	}
//
//	return nRetVal;
//}
//
//////////////////////////////////////////////////////////////
//// Gets the start of the partion space                    //
//////////////////////////////////////////////////////////////
//u64 CWIIDisc::GetFreePartitionStart(image_file *image)
//{
//	u64 nRetVal;
//
//	if (1==image->nparts)
//	{
//		// default offset of 0x50000
//
//		nRetVal = 0x50000;
//	}
//	else
//	{
//		// get the first free byte at the end
//		nRetVal = image->parts[image->nparts-1].offset +  image->parts[image->nparts-1].data_offset
//				  + image->parts[image->nparts-1].data_size;
//	}
//
//
//
//	return nRetVal;
//}
/////////////////////////////////////////////////////////////
// Goes through the partitions moving them up and updating //
// the partition table                                     //
/////////////////////////////////////////////////////////////

//BOOL CWIIDisc::DoTheShuffle(image_file *image)
//{
//
//	//BOOL bRetVal = FALSE;
//	//u64 nStoreLocation = 0x50000;
//	//u64 nPartitionStart;
//	//u64 nLength = 0;
//	//u64 nWriteLocation;
//
//	//u8	nBuffer[4];
//
//	//for (unsigned int i=1; i < image->nparts; i++)
//	//{
//
//	//	// get the length and start of the partition
//	//	nPartitionStart = image->parts[i].offset;
//	//	nLength = image->parts[i].data_size + 0x20000;
//
//	//	// check to see if we need to move it
//	//	if (nPartitionStart != nStoreLocation)
//	//	{
//	//		// move the partition down
//
//	//		if (FALSE==CopyDiscDataDirect(image, i, nPartitionStart, nStoreLocation, nLength))
//	//		{
//	//			// cancelled
//	//			return FALSE;
//	//		}
//	//		// show we have modified something
//
//	//		bRetVal = TRUE;
//	//		Write32(nBuffer, (u32)(nStoreLocation >> 2));
//	//		// update the correct table
//	//		if (i > image->PartitionCount)
//	//		{
//	//			// use the channel table
//	//			// create the updated channel list in the correct location on the disc 
//	//			nWriteLocation = image->chan_tbl_offset + (u64)(8)*(u64)(i - image->PartitionCount -1);
//	//			DiscWriteDirect(image, nWriteLocation, nBuffer, 4);
//	//		}
//	//		else
//	//		{
//	//			// use the partition table
//	//			nWriteLocation = image->part_tbl_offset + (u64)(8)*(u64)(i-1);
//	//			DiscWriteDirect(image, nWriteLocation, nBuffer, 4);
//	//		}
//	//	}
//	//	nStoreLocation = nLength + nStoreLocation;
//	//}
//	//return bRetVal;
//}

//////////////////////////////////////////////////////////////
// Copy data between two parts of the disc image            //
//////////////////////////////////////////////////////////////
//BOOL CWIIDisc::CopyDiscDataDirect(image_file * image, int nPart, u64 nSource, u64 nDest, u64 nLength)
//{
// //   MSG msg;
//
//	//// optomise for 32k chunks
//	//u64 nCount;
//	//u32 nBlocks = 0;
//	//u32 nBlockCount = 0;
//	//u32 nReadCount = 0;
//
//	//u8 * pData;
//
//	//// try and use 1 meg at a time
//	//pData = (u8 *)malloc(0x100000);
//
//	//nCount = 0;
//	//nBlocks =0;
//	//nBlockCount = (u32)((nLength / 0x100000) + 1);
//
//	//// now open a progress bar
//	//CProgressBox * pProgressBox = new CProgressBox(m_pParent);
//	//
//	//pProgressBox->Create(IDD_PROGRESSBOX);
//	//
//	//pProgressBox->ShowWindow(SW_SHOW);
//	//pProgressBox->SetRange(0, nBlockCount);
//	//
//	//pProgressBox->SetPosition(0);
//	//
//	//CString csTempString;
//
//	//csTempString.Format("Copying down partition %d", nPart);
//	//pProgressBox->SetWindowMessage(csTempString);
//	//
//	//// now the loop
//	//while (nCount < nLength)
//	//{
//
//	//	pProgressBox->SetPosition(nBlocks);
//	//	
//	//	nReadCount = 0x100000;
//	//	if (nReadCount > (nLength-nCount))
//	//	{
//	//		nReadCount = (u32)(nLength - nCount);
//	//	}
//
//	//	io_read(pData, nReadCount, image, nSource);
//
//	//	// usual message pump
//	//	if (PeekMessage(&msg,
// //           NULL,
// //           0,
// //           0,
// //           PM_REMOVE))
// //       {
// //           // PeekMessage has found a message--process it 
// //           if (msg.message != WM_CANCELLED)
// //           {
// //               TranslateMessage(&msg); // Translate virt. key codes 
// //               DispatchMessage(&msg);  // Dispatch msg. to window 
// //           }
//	//		else
//	//		{
//	//			AddToLog("Cancelled - probably unusable now");
//	//			delete pProgressBox;
//	//			return FALSE;
//	//		}
// //       }
//	//	
//
//	//	DiscWriteDirect(image, nDest, pData, nReadCount);
//
//	//	nDest = nDest + nReadCount;
//	//	nSource = nSource + nReadCount;
//	//	nBlocks++;
//	//	nCount = nCount + nReadCount;
//	//}
//	//delete pProgressBox;
//	//
//	//
//	//free(pData);
//	//return TRUE;
//}
////////////////////////////////////////////////////////////////////
// Save a decrypted partition out                                 //
////////////////////////////////////////////////////////////////////
BOOL CWIIDisc::SaveDecryptedPartition(const char* csName, image_file *image, u32 nPartition)
{
   //MSG msg;

	// now open a progress bar
	u64	nStartOffset;
	u64 nLength;
	u64 nOffset = 0;

	u8 * pData;
	FILE * fOut;
	u32 nBlockCount = 0;

	fOut = fopen(csName, "wb");

	if (NULL==fOut)
	{
		// Error
		return FALSE;
	}

	// now get the parameters we need to save
	nStartOffset = image->parts[nPartition].offset;
	nLength = image->parts[nPartition].data_size;

	pData = (u8 *)malloc(0x20000);

	// save the first 0x20000 bytes direct as thats the partition.bin
	io_read(pData,0x20000, image, nStartOffset);
	fwrite(pData,1, 0x20000, fOut);

	//CProgressBox * pProgressBox = new CProgressBox(m_pParent);
	//
	//pProgressBox->Create(IDD_PROGRESSBOX);
	//
	//pProgressBox->ShowWindow(SW_SHOW);
	//pProgressBox->SetRange(0, (u32)(nLength / 0x8000));
	//
	//pProgressBox->SetPosition(0);
	//
	//pProgressBox->SetWindowMessage("Saving partition");
	// then step through the clusters
	nStartOffset = 0;
	for (u64 nCount = 0; nCount < nLength; nCount = nCount + 0x8000)
	{
		//pProgressBox->SetPosition(nBlockCount);
		io_read_part(pData, 0x7c00, image, nPartition, nOffset);
		// usual message pump
		//if (PeekMessage(&msg,
  //          NULL,
  //          0,
  //          0,
  //          PM_REMOVE))
  //      {
  //          // PeekMessage has found a message--process it 
  //          if (msg.message != WM_CANCELLED)
  //          {
  //              TranslateMessage(&msg); // Translate virt. key codes 
  //              DispatchMessage(&msg);  // Dispatch msg. to window 
  //          }
		//	else
		//	{
		//		AddToLog("Cancelled save");
		//		//delete pProgressBox;
		//		free(pData);
		//		return FALSE;
		//	}
  //      }
		fwrite(pData, 1,0x7c00, fOut);
		nOffset = nOffset + 0x7c00;
		nBlockCount++;
	}

	//delete pProgressBox;
	fclose(fOut);
	free(pData);
	return TRUE;
}
////////////////////////////////////////////////////////
// Load a decrypted partition of data and fill the    //
// partition up with it                               //
////////////////////////////////////////////////////////
//BOOL CWIIDisc::LoadDecryptedPartition(CString csName, image_file *image, u32 nPartition)
//{
//
//	// now open a progress bar
//	u64	nStartOffset;
//	u64 nLength;
//
//	u8 * pData;
//	FILE * fIn;
//	u32 nBlockCount = 0;
//
//	u64 nFileSize;
//
//	fIn = fopen(csName, "rb");
//
//	if (NULL==fIn)
//	{
//		// Error
//		return FALSE;
//	}
//
//	// now get the parameters we need to save
//	nStartOffset = image->parts[nPartition].offset;
//	nLength = image->parts[nPartition].data_size;
//
//
//	// now check the size of the file we are trying to read in
//	nFileSize = _lseeki64(fIn->_file, 0L, SEEK_END);
//	_lseeki64(fIn->_file, 0L, SEEK_SET);
//
//	// now account for the partition header and the actual number of clusters of data
//	if (nLength < (((nFileSize - 0x20000)/0x7c00)* 0x8000))
//	{
//		// not enough space for the partition load
//		AfxMessageBox("File too big to load into partition");
//		fclose(fIn);
//		return FALSE;
//	}
//
//	pData = (u8 *)malloc(0x20000);
//
//	// save the first 0x20000 bytes direct as thats the partition.bin
//	fread(pData,1,0x20000, fIn);
//	DiscWriteDirect(image, nStartOffset, pData, 0x20000);
//
//	// now really need to parse the header for the new partition as the
//	// title key etc. will be different
//	get_partitions(image);
//
//	// now write the file out
//	if (FALSE==wii_write_data_file(image, nPartition, 0, nFileSize-0x20000,NULL, fIn))
//	{
//		fclose(fIn);
//		free(pData);
//		return FALSE;	
//	}
//
//
//	fclose(fIn);
//	free(pData);
//	return TRUE;	
//}
//
////////////////////////////////////////////////////////////////////////
//// Shrink the data up in the partition                              //
//// we just move the data up in the partition by finding out where   //
//// the free space in the middle is and copying the data down from   //
//// above it                                                         //
//// we then update the fst.bin to take off however much we did       //
//// to save time we copy from one cluster group star to another as   //
//// then we don't need to recalculate the sha tables, just copy them //
//// we also need to sign at the end                                  //
////////////////////////////////////////////////////////////////////////
//
////BOOL CWIIDisc::DoPartitionShrink(image_file *image, u32 nPartition)
////{
////
////	u64 nClusterSource;
////	u64 nSourceDataOffset;
////	u64 nClusterDestination;
////	u64 nDestinationDataOffset;
////
////	u64 nSourceClusterGroup;
////	u64 nDestClusterGroup;
////	u64 nClusterGroups;
////
////	u32 nDifference;
////
////	u64 i;
////
////	// allocate space for the fst.bin
////
////	u8 * pFST = (u8 *)malloc((u32)(image->parts[nPartition].header.fst_size));
////
////	// allocate space for the data size (as we modify it
////	u8 nDataSize[4];
////	
////	// read the fst.bin and data size files
////	io_read_part(pFST, (u32)(image->parts[nPartition].header.fst_size), image, nPartition, image->parts[nPartition].header.fst_offset);
////	io_read(nDataSize, 0x0004, image, image->parts[nPartition].offset + 0x2bC);
////
////	// find the first empty block from main.dol onwards
////	nClusterDestination = FindFirstData(image->parts[nPartition].offset + image->parts[nPartition].data_offset+image->parts[nPartition].header.dol_offset,
////													image->parts[nPartition].data_size-image->parts[nPartition].header.dol_offset, FALSE);
////
////	// check for error condition
////	if (0==nClusterDestination)
////	{
////		AfxMessageBox("Unable to find space to remove or main.dol incorrect");
////		free(pFST);
////		return FALSE;
////	}
////
////	// change it to a higher cluster boundary
////	nDestinationDataOffset = nClusterDestination - (image->parts[nPartition].offset + image->parts[nPartition].data_offset);
////	nDestClusterGroup = (nDestinationDataOffset / (0x8000*NB_CLUSTER_GROUP))+1;
////	nDestinationDataOffset = nDestClusterGroup * NB_CLUSTER_GROUP * 0x7c00;
////	nClusterDestination = nDestClusterGroup * NB_CLUSTER_GROUP * 0x8000 + image->parts[nPartition].offset + image->parts[nPartition].data_offset;
////
////	// now find the start of the data
////	nClusterSource = FindFirstData(nClusterDestination,
////								   image->parts[nPartition].data_size - nDestClusterGroup* NB_CLUSTER_GROUP * 0x8000,
////								   TRUE);
////
////	if (0==nClusterSource)
////	{
////		AfxMessageBox("Unable to find space to remove or main.dol incorrect");
////		free(pFST);
////		return FALSE;
////
////	}
////	// change to a lower cluster boundary
////	nSourceDataOffset = nClusterSource - (image->parts[nPartition].offset + image->parts[nPartition].data_offset);
////	nSourceClusterGroup = ((nSourceDataOffset/ 0x8000)/NB_CLUSTER_GROUP);
////	nSourceDataOffset = nSourceClusterGroup * NB_CLUSTER_GROUP * 0x7c00;
////	nClusterSource = nSourceClusterGroup * NB_CLUSTER_GROUP * 0x8000 + image->parts[nPartition].offset + image->parts[nPartition].data_offset;
////
////	// calculate number we need to copy
////	nClusterGroups = (image->parts[nPartition].data_size / (0x8000 * NB_CLUSTER_GROUP)) - nSourceClusterGroup;
////
////
////	// check to see if it's worth doing
////
////	if (nSourceClusterGroup==nDestClusterGroup)
////	{
////		// same source/dest so pointless
////		AfxMessageBox("Pointless doing it as source and dest are the same group");
////		free(pFST);
////		return FALSE;
////	}
////
////	// read the h3 table
////	io_read(h3, SIZE_H3, image, image->parts[nPartition].offset + image->parts[nPartition].h3_offset);
////
////	// move the data down
////	CopyDiscDataDirect(image, nPartition, nClusterSource, nClusterDestination, nClusterGroups*0x8000*NB_CLUSTER_GROUP);
////
////	// update the h3 and save out as the write file use it
////	for (i = 0; i < nClusterGroups; i++)
////	{
////		memcpy(&h3[(nDestClusterGroup+i)* 0x14], &h3[(nSourceClusterGroup +i)*0x14], 0x14);
////	}
////	DiscWriteDirect(image, image->parts[nPartition].offset + image->parts[nPartition].h3_offset, h3, SIZE_H3);
////
////	// now update the fst table entries
////	nDifference = (u32)(((nSourceClusterGroup - nDestClusterGroup) * NB_CLUSTER_GROUP * 0x7c00) >> 2);
////
////	u32 nFSTEntries  = be32(pFST + 8);
////	u32 nTempOffset;
////
////	for (i = 0; i < nFSTEntries; i++)
////	{
////		// if a file
////		if (pFST[i*0x0C]==0x00)
////		{
////			// get current offset
////			nTempOffset = be32(pFST + i*0x0c + 4);
////			// take off difference
////			nTempOffset = nTempOffset - nDifference;
////			// save entry
////			Write32(pFST + i*0x0c + 4,nTempOffset);
////		}
////	}
////	// save the fst.bin
////	wii_write_data_file(image, nPartition, image->parts[nPartition].header.fst_offset, image->parts[nPartition].header.fst_size, pFST);
////
////	// update the data size in boot.bin
////	u32 nSize = be32(nDataSize);
////
////	nDifference = (u32)(((nSourceClusterGroup - nDestClusterGroup) * NB_CLUSTER_GROUP * 0x8000) >> 2);
////
////	Write32(nDataSize, nSize - nDifference);
////
////	// save it
////
////	DiscWriteDirect(image, image->parts[nPartition].offset + 0x2bc, nDataSize, 4);
////	// sign it
////
////	wii_trucha_signing(image, nPartition);
////	
////	free(pFST);
////	// free the memory
////	return TRUE;
////}
//
////////////////////////////////////////////////////////////////////////
//// Search for the first block of data that is marked as either used //
//// or unused                                                        //
////////////////////////////////////////////////////////////////////////
//u64 CWIIDisc::FindFirstData(u64 nStartOffset, u64 nLength, BOOL bUsed)
//{
//	u64 nBlock = nStartOffset / 0x8000;
//	u64 nEndBlock = (nStartOffset + nLength - 1) / 0x8000;
//
//	while(nBlock <nEndBlock)
//	{
//		if (TRUE==bUsed)
//		{
//			if (1==pFreeTable[nBlock])
//			{
//				return nBlock * 0x8000;
//			}
//		}
//		else
//		{
//			if (0==pFreeTable[nBlock])
//			{
//				return nBlock * 0x8000;
//			}
//		}
//		nBlock ++;
//	}
//	return 0;
//
//}
//

///////////////////////////////////////////////////////////////
// Save all the files in a partition to the passed directory //
///////////////////////////////////////////////////////////////
BOOL CWIIDisc::ExtractPartitionFiles(image_file *image, u32 nPartition, u8 *cDirPathName)
{
	u8 * fst;
	u32 nfiles;

	// get the working directory
	char buffer[_MAX_PATH];

   // Get the current working directory:
   if( _getcwd( buffer, _MAX_PATH ) == NULL )
   {
      //AddToLog("_getcwd error" );
	  return FALSE;
	}
	// change to the new directory

   _chdir((char *)cDirPathName);
    fst = (u8 *) (malloc ((u32)(image->parts[nPartition].header.fst_size)));

    if (io_read_part (fst, (u32)(image->parts[nPartition].header.fst_size),image, nPartition, image->parts[nPartition].header.fst_offset) !=
                            image->parts[nPartition].header.fst_size)
	{
           //AfxMessageBox("fst.bin");
		free (fst);
		return FALSE;
    }

    nfiles = be32 (fst + 8);

	// create a progress window and pass the value to the parse function
	// where the message pump will run
	//CProgressBox * pProgressBox;
	//
	//pProgressBox = new CProgressBox(m_pParent); 
	//
	//pProgressBox->Create(IDD_PROGRESSBOX);
	//
	//pProgressBox->ShowWindow(SW_SHOW);
	//pProgressBox->SetRange(0, nfiles);
	//
	//pProgressBox->SetPosition(0);
	//
	//pProgressBox->SetWindowMessage("Saving partition of data. Please wait");

	u32 nFiles = parse_fst_and_save(fst, (char *) (fst + 12 * nfiles), 0 , image, nPartition);

	//delete pProgressBox;
	free(fst);
    //change back to the working directory
	_chdir(buffer);

	if (nFiles != nfiles)
	{
		//AddToLog("Error writing files out");
		return FALSE;
	}
	return TRUE;
}
