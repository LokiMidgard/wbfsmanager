// This is the main DLL file.

#include "stdafx.h"
#include <ctype.h>
#include "wiiscrubberWrapper.h"
extern "C"
{
__declspec(dllexport) int ExtractOpeningBnr(char* isoFileName, char* pathToKey, char* targetFilename)
{
	CWIIDisc* pcWiiDisc=new CWIIDisc();
	image_file* pImageFile;
	pcWiiDisc->Reset();
	pcWiiDisc->pathToCommonKey= pathToKey;
	pImageFile = pcWiiDisc->image_init(isoFileName);
	if (NULL==pImageFile)
	{
		return -1;
	}
	pcWiiDisc->image_parse(pImageFile);
	u64 nTestCase;
		
	nTestCase = (u64) pcWiiDisc->CountBlocksUsed();
	
	nTestCase *= (u64)(0x8000);
	nTestCase = nTestCase + ((pcWiiDisc->nImageSize - nTestCase)/32);
	// find out the partition with data in
	unsigned int x = 0;
	while (x < pImageFile->nparts)
	{
		if (PART_DATA == pImageFile->parts[x].type)
		{
			break;
		}
		x ++;
	}
	if (x==pImageFile->nparts)
	{
		// error as we have no data here?
		pcWiiDisc->image_deinit(pImageFile);
		return -2;
	}
	unsigned int activePartition = x;
	// now clear out the junk data in the name
	for (int i=0; i< 0x60; i++)
	{
		if (0==isprint((int) pImageFile->parts[x].header.name[i]))
		{
			pImageFile->parts[x].header.name[i] = ' ';
		}
		
	}
	
	BOOL openingBnrFound=FALSE;
	for(int i=0; i<pImageFile->nparts;i++)
	{
		if(pImageFile->parts[i].type==PART_DATA)
		{
			if(pcWiiDisc->ExportFile("opening.bnr", targetFilename,pImageFile, i, true))
			{
				openingBnrFound=TRUE;
				break;
			}
		}
	}
	pcWiiDisc->image_deinit(pImageFile);
	if(openingBnrFound)
		return 0;
	return -4;
}
}

//CWIIDisc	*	pcWiiDisc;
//struct image_file  * pImageFile;
//int m_nWorkingPartition;
//void ParseDiscDetails(void);
//BOOL m_bISOLoaded;