#include "WbfsInterm.h"

extern "C"
{
#include "DiskAccessTools.h"
#include "libwbfsExt.h"
#define GB (1024 * 1024 * 1024.)
#define MB (1024 * 1024)
#define PARTITIONNOTLOADED -1
	wbfs_t* partitionPtr=NULL;

	fatal_error_callback_t fatal_error=NULL;
	__declspec(dllexport) bool OpenDrive(char* partitionLetter)
	{
		partitionPtr = wbfs_try_open(NULL, partitionLetter, FALSE);
		if(partitionPtr==NULL)
			return false;
		return true;
	}

	__declspec(dllexport) void CloseDrive()
	{
		if(partitionPtr==NULL)
			return;
		if(partitionPtr->callback_data==NULL)
			return;
		wbfs_close(partitionPtr);
		partitionPtr=NULL;		// TODO: Added, dangerous!
	}

	__declspec(dllexport) bool FormatDrive(char* partitionLetter)
	{
		partitionPtr = wbfs_try_open(NULL, partitionLetter, TRUE);
		if(partitionPtr==NULL)
			return false;
		return true;
	}

	/* return values:
	/// -1 : Partition wasn't loaded previously
	*/
	__declspec(dllexport) int GetDiscCount()
	{
		if(partitionPtr==NULL)
			return PARTITIONNOTLOADED;
		return wbfs_count_discs(partitionPtr);
	}

	/* return values:
	/// -1 : Partition wasn't loaded previously
	/// -2 : Invalid index
	/// -3 : Failure reading disc info
	*/
	__declspec(dllexport) int GetDiscInfo(int index, char* discId, float* sizeGB, char* discName)
	{
		if(partitionPtr==NULL)
			return PARTITIONNOTLOADED;
		
		unsigned char *b = (unsigned char*)wbfs_ioalloc(0x100);
		if(index<0)
			return -2;
		unsigned int* size=new unsigned int;
		
		if (wbfs_get_disc_info(partitionPtr, index, b, 0x100, size))
		{
			return -3;
		}
		
		if(sizeGB==NULL)
			sizeGB=new float;
		*sizeGB=*size* 4ULL / (GB);

		if(discId==NULL)
			discId=new char[7];
		for(int i=0;i<7;i++)
		{
			discId[i]=b[i];
		}

		int counter=0x20;
		if(discName==NULL)
		{
			int length=CustomStrLenU(b+0x20);
			/*int length=0;
			while(b[counter++]!='\0')
			{
				length++;
			}*/
			discName=new char[length];
		}
		counter=0x20;
		do
		{
			discName[counter-0x20]=b[counter];
		}while(b[counter++]!='\0');
		wbfs_iofree(b);
		return 0;
	}

	/* return values:
	/// -1 : Partition wasn't loaded previously
	/// -2 : Invalid index
	/// -3 : Failure reading disc info
	*/
	__declspec(dllexport) int GetDiscInfoEx(int index, char* discId, float* sizeGB, char* discName, RegionCode* regionCode)
	{
		if(partitionPtr==NULL)
			return PARTITIONNOTLOADED;
		
		unsigned char *b = (unsigned char*)wbfs_ioalloc(0x100);
		if(index<0)
			return -2;
		unsigned int* size=new unsigned int;
		
		if (wbfs_get_disc_info(partitionPtr, index, b, 0x100, size))
		{
			return -3;
		}
		
		if(sizeGB==NULL)
			sizeGB=new float;
		*sizeGB=*size* 4ULL / (GB);

		if(regionCode!=NULL)
		{
			//if(b[0x3]=='P')
			//	*regionCode = PAL;
			//else if(b[0x3]=='J')
			//	*regionCode = NTSCJ;
			//else if(b[0x3]=='K')
			//	*regionCode = KOR;
			//else
			//	*regionCode = NTSC;
			if(b[0x3]=='E')
				*regionCode = NTSC;
			else if(b[0x3]=='P' || b[0x3]=='F'  || b[0x3]=='D' || b[0x3]=='L')
				*regionCode = PAL;
			else if(b[0x3]=='J')
				*regionCode = NTSCJ;
			else if(b[0x3]=='K' || b[0x3]=='Q'  || b[0x3]=='T' )
				*regionCode = KOR;
			else
				*regionCode = NOREGION;
		}

		if(discId==NULL)
			discId=new char[7];
		for(int i=0;i<7;i++)
		{
			discId[i]=b[i];
		}

		int counter=0x20;
		if(discName==NULL)
		{
			int length=CustomStrLenU(b+0x20);
			/*int length=0;
			while(b[counter++]!='\0')
			{
				length++;
			}*/
			discName=new char[length];
		}
		counter=0x20;
		do
		{
			discName[counter-0x20]=b[counter];
		}while(b[counter++]!='\0');
		wbfs_iofree(b);
		return 0;
	}

	/* return values:
	/// -1 : Partition wasn't loaded previously
	*/
	__declspec(dllexport) int GetUsedBlocksCount()
	{
		if(partitionPtr==NULL)
			return PARTITIONNOTLOADED;
		return wbfs_count_usedblocks(partitionPtr);
	}

	/* return values:
	/// -1 : Partition wasn't loaded previously
	/// -2 : Error occured while attempting to read file
	*/
	__declspec(dllexport) int GetDiscImageInfo(const char* filename, char* discId, float* estimatedSize, char* discName, partition_selector_t partitionSelection)
	{
		if(partitionPtr==NULL)
			return PARTITIONNOTLOADED;
		HANDLE* handle = CreateFileHP(filename, GENERIC_READ, 0, NULL, OPEN_EXISTING, 0, NULL);

		if (handle == INVALID_HANDLE_VALUE)
		{
			return -2;
		}
		unsigned char *header = (unsigned char*)wbfs_ioalloc(0x100);
		unsigned int estimation = wbfs_estimate_disc(partitionPtr, read_wii_disc_sector, handle, partitionSelection, header);
		if(estimatedSize==NULL)
			estimatedSize=new float;
		*estimatedSize=estimation* 1.0 / (GB);

		if(discId==NULL)
			discId=new char[7];
		for(int i=0;i<7;i++)
		{
			discId[i]=header[i];
		}

		int counter=0x20;
		if(discName==NULL)
		{
			int length=CustomStrLenU(header+0x20);
			/*int length=0;
			while(b[counter++]!='\0')
			{
				length++;
			}*/
			discName=new char[length];
		}
		counter=0x20;
		do
		{
			discName[counter-0x20]=header[counter];
		}while(header[counter++]!='\0');
		wbfs_iofree(header);
		CloseHandle(handle);
		return 0;
		// (estimation*1.0) / (GB);
	}

	/* return values:
	/// -1 : Partition wasn't loaded previously
	/// -2 : Error occured while attempting to read file
	*/
	__declspec(dllexport) int GetDiscImageInfoEx(const char* filename, char* discId, float* estimatedSize, char* discName, RegionCode* regionCode, partition_selector_t partitionSelection)
	{
		if(partitionPtr==NULL)
			return PARTITIONNOTLOADED;
		HANDLE* handle = CreateFileHP(filename, GENERIC_READ, 0, NULL, OPEN_EXISTING, 0, NULL);

		if (handle == INVALID_HANDLE_VALUE)
		{
			return -2;
		}
		unsigned char *header = (unsigned char*)wbfs_ioalloc(0x100);
		float estimation = wbfs_estimate_disc(partitionPtr, read_wii_disc_sector, handle, partitionSelection, header);
		if(estimatedSize==NULL)
			estimatedSize=new float;
		*estimatedSize=estimation* 1.0 / (GB);

		if(discId==NULL)
			discId=new char[7];
		for(int i=0;i<7;i++)
		{
			discId[i]=header[i];
		}

		/*
		ASCII Hex Region
		A 41 All regions. System channels like the Mii channel use it.
		D 44 German-speaking regions. Only if separate versions exist, e.g. Zelda: A Link to the Past
		E 45 USA and other NTSC regions except Japan
		F 46 French-speaking regions. Only if separate versions exist, e.g. Zelda: A Link to the Past.
		J 4A Japan
		K 4B Korea
		L 4C PAL/World?
		P 50 Europe, Australia and other PAL regions
		Q 51 Korea with Japanese language.
		T 54 Korea with English language.
		X 58 Not a real region code. Homebrew Channel uses it, though. 
			*/
		if(regionCode!=NULL)
		{
			if(header[0x3]=='E')
				*regionCode = NTSC;
			else if(header[0x3]=='P' || header[0x3]=='F'  || header[0x3]=='D' || header[0x3]=='L')
				*regionCode = PAL;
			else if(header[0x3]=='J')
				*regionCode = NTSCJ;
			else if(header[0x3]=='K' || header[0x3]=='Q'  || header[0x3]=='T' )
				*regionCode = KOR;
			else
				*regionCode = NOREGION;
		}

		int counter=0x20;
		if(discName==NULL)
		{
			int length=CustomStrLenU(header+0x20);
			/*int length=0;
			while(b[counter++]!='\0')
			{
				length++;
			}*/
			discName=new char[length];
		}
		counter=0x20;
		do
		{
			discName[counter-0x20]=header[counter];
		}while(header[counter++]!='\0');
		wbfs_iofree(header);
		CloseHandle(handle);
		return 0;
		// (estimation*1.0) / (GB);
	}
	
	/* return values:
	/// -1 : Partition wasn't loaded previously
	/// -2 : Error occured while attempting to read file
	/// -3 : Disc already exists on WBFS drive
	*/
	__declspec(dllexport) int AddDiscToDrive(char* filename, progress_callback_t progressCallback, partition_selector_t wiiPartitionToAdd, bool copy1to1, char *newName)
	{
		if(partitionPtr==NULL)
			return PARTITIONNOTLOADED;

		u8 discinfo[7];
		wbfs_disc_t *d;
		HANDLE* handle = CreateFileHP(filename, GENERIC_READ, 0, NULL, OPEN_EXISTING, 0, NULL);
		
		if (handle == INVALID_HANDLE_VALUE)
		{
			return -2;
		}
		DWORD read = 0;
		ReadFile(handle, discinfo, 6, &read, NULL);

		d = wbfs_open_disc(partitionPtr, discinfo);
		
		if (d)
		{
			wbfs_close_disc(d);
			CloseHandle(handle);
			return -3;
		}
		else
		{
			u32 res=wbfs_add_disc(partitionPtr, read_wii_disc_sector, handle, progressCallback, wiiPartitionToAdd, copy1to1, newName==NULL ? NULL : (CustomStrLen(newName)>1) ? newName : NULL);
		}

		CloseHandle(handle);
		return 0;
	}

	/* return values:
	/// -1 : Partition wasn't loaded previously
	/// -2 : File could not be found on WBFS drive
	*/
	__declspec(dllexport) int RemoveDiscFromDrive(char* discId)
	{
		if(partitionPtr==NULL)
			return PARTITIONNOTLOADED;
			
		if(wbfs_rm_disc(partitionPtr, (unsigned char *)(discId)))
		{
			return -2;
		}
		return 0;
	}

	/* return values:
	/// -1 : Partition wasn't loaded previously
	/// -2 : File could not be found on WBFS drive
	/// -3 : Unable to open file on disk for writing
	*/
	__declspec(dllexport) int ExtractDiscFromDrive(char* discId, progress_callback_t progressCallback, char* targetName)
	{
		if(partitionPtr==NULL)
			return PARTITIONNOTLOADED;

		wbfs_disc_t *d;
		d = wbfs_open_disc(partitionPtr, (unsigned char *)(discId));
		
		if(!d)
			return -2;

		HANDLE* handle;
		char isoname[0x100];
		int i,len;
		
		if(CustomStrLen(targetName)>0)
		{
			wbfs_memset(isoname, 0, sizeof(isoname));
			strncpy(isoname, targetName, 0x100);
			len = strlen(isoname);
		}
		else
		{
			/* get the name of the title to find out the name of the iso */
			strncpy(isoname, (char *)d->header->disc_header_copy + 0x20, 0x100);
			len = strlen(isoname);
		
			// replace silly chars with '_'
			for (i = 0; i < len; i++)
			{
				if (isoname[i] == '/' || isoname[i] == ':' || isoname[i]=='\\' || isoname[i]=='*'|| isoname[i]=='?'|| isoname[i]=='"'|| isoname[i]=='<'|| isoname[i]=='>'|| isoname[i]=='!')
				{
					isoname[i] = '_';
				}
			}
			strncpy(isoname + len, ".iso", 0x100 - len);
		}
		
		handle = CreateFileHP(isoname, GENERIC_READ | GENERIC_WRITE, 0, NULL, CREATE_NEW, 0, NULL);
		
		if (handle == INVALID_HANDLE_VALUE)
		{
			return -3;
		}
		LARGE_INTEGER large;


		large.QuadPart = (d->p->n_wii_sec_per_disc / 2) * 0x8000ULL;
		SetFilePointerEx(handle, large, NULL, FILE_BEGIN);
		SetEndOfFile(handle);

		wbfs_extract_disc(d,write_wii_disc_sector, handle, progressCallback);
		
		CloseHandle(handle);
			
		wbfs_close_disc(d);

		return 0;
	}

	/* return values:
	/// -1 : Partition wasn't loaded previously
	/// -2 : Error occured while renaming
	*/
	__declspec(dllexport) int RenameDiscOnDrive(char* discId, char* newName)
	{
		if(partitionPtr==NULL)
			return PARTITIONNOTLOADED;
		if(wbfs_ren_disc(partitionPtr, (unsigned char*)discId, newName))
		{
			return -2;
		}
	}

	__declspec(dllexport) void SubscribeErrorEvent(fatal_error_callback_t errorEventHandler)
	{
		fatal_error = errorEventHandler;
	}


	/* return values:
	/// -1 : Partition wasn't loaded previously
	*/
	__declspec(dllexport) int GetDriveStats(unsigned int* blocks, float* total, float* used, float* free)
	{
		if(partitionPtr==NULL)
			return PARTITIONNOTLOADED;

		if(blocks==NULL)
			blocks=new unsigned int;
		*blocks = wbfs_count_usedblocks(partitionPtr);
		if(total==NULL)
			total=new float;
		*total = (float)partitionPtr->n_wbfs_sec * partitionPtr->wbfs_sec_sz / GB;
		if(used==NULL)
			used = new float;
		*used = (float)(partitionPtr->n_wbfs_sec-*blocks) * partitionPtr->wbfs_sec_sz / GB;
		if(free==NULL)
			free = new float;
		*free = (float)(*blocks) * partitionPtr->wbfs_sec_sz / GB;
		return 0;
	}

	__declspec(dllexport) int DriveToDriveSingleCopy(char* targetPartitionLetter, progress_callback_t progressCallback/*, partition_selector_t wiiPartitionToAdd*/, char* discId)
	{
		if(partitionPtr==NULL)
			return PARTITIONNOTLOADED;
		wbfs_t* targetPartitionPtr = wbfs_try_open(NULL, targetPartitionLetter, FALSE);
		if(targetPartitionPtr==NULL)
			return -2;
		wbfs_disc_t *discPtr;
		discPtr = wbfs_open_disc(targetPartitionPtr, (unsigned char *)(discId));
		if(discPtr)
		{
			//Already on drive
			wbfs_close_disc(discPtr);
			wbfs_close(targetPartitionPtr);
			return -3;
		}
		discPtr = wbfs_open_disc(partitionPtr, (unsigned char *)(discId));
		if(discPtr)
		{
			int result = wbfs_driveToDriveCopy(partitionPtr, targetPartitionPtr, discPtr, progressCallback/*, wiiPartitionToAdd*/);
			wbfs_close_disc(discPtr);
			wbfs_close(targetPartitionPtr);
			if(result==0)
				return result;
			else
				return result-100;
		}
		else
		{
			wbfs_close(targetPartitionPtr);
			return -4;
		}
	}

	__declspec(dllexport) int CanDoDirectDriveToDrive(char* targetPartitionLetter)
	{
		if(partitionPtr==NULL)
			return PARTITIONNOTLOADED;
		wbfs_t* targetPartitionPtr = wbfs_try_open(NULL, targetPartitionLetter, FALSE);
		if(targetPartitionPtr==NULL)
			return -2;
		if((targetPartitionPtr->wbfs_sec_sz/targetPartitionPtr->hd_sec_sz)!=(partitionPtr->wbfs_sec_sz/partitionPtr->hd_sec_sz))
		{	
			wbfs_close(targetPartitionPtr);
			return -3;
		}
		wbfs_close(targetPartitionPtr);
		return 0;
	}

	void fatal(const char *s, ...)
	{
		if(fatal_error!=NULL)
			(*fatal_error)(s);
	}
	void non_fatal(const char *s, ...)
	{
		//if(fatal_error!=NULL)
		//	(*fatal_error)(s);
	}


	int CustomStrLenU(unsigned char* data)
	{
		int counter=0;
		int length=0;
		while(data[counter++]!='\0')
		{
			length++;
		}
		return length;
	}
	int CustomStrLen(char* data)
	{
		int counter=0;
		int length=0;
		while(data[counter++]!='\0')
		{
			length++;
		}
		return length;
	}


	
}