#pragma once

// WIIDisc.h: interface for the CWIIDisc class.
//
//////////////////////////////////////////////////////////////////////

#if !defined(AFX_WIIDISC_H__23AF31B0_FEBE_4B6D_ABA6_D778D6DA8683__INCLUDED_)
#define AFX_WIIDISC_H__23AF31B0_FEBE_4B6D_ABA6_D778D6DA8683__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000
#include "stdafx.h"
#include <sys/types.h>
#include <sys/stat.h>
#include <string.h>

#include "aes.h"

#include "global.h"	// Added by ClassView
//#include "ProgressBox.h"

#define SIZE_H0						0x0026CUL
#define SIZE_H1						0x000A0UL
#define SIZE_H2						0x000A0UL
#define SIZE_H3						0x18000UL
#define SIZE_H4						0x00014UL
#define SIZE_PARTITION_HEADER		0x20000UL
#define SIZE_CLUSTER				0x08000UL
#define SIZE_CLUSTER_HEADER			0x00400UL
#define SIZE_CLUSTER_DATA			(SIZE_CLUSTER - SIZE_CLUSTER_HEADER)

/*
 * ADDRESSES
 */
/* Absolute addresses */
#define OFFSET_GAME_TITLE			0x00020UL
#define OFFSET_PARTITIONS_INFO		0x40000UL
#define OFFSET_REGION_BYTE			0x4E003UL
#define OFFSET_REGION_CODE			0x4E010UL
/* Relative addresses */
#define OFFSET_H0					0x00000UL
#define OFFSET_H1					0x00280UL
#define OFFSET_H2					0x00340UL
#define OFFSET_PARTITION_TITLE_KEY	0x001BFUL
#define OFFSET_PARTITION_TITLE_ID	0x001DCUL
#define OFFSET_PARTITION_TMD_SIZE	0x002A4UL
#define OFFSET_PARTITION_TMD_OFFSET	0x002A8UL
#define OFFSET_PARTITION_H3_OFFSET	0x002B4UL
#define OFFSET_PARTITION_INFO		0x002B8UL
#define OFFSET_CLUSTER_IV			0x003D0UL
#define OFFSET_FST_NB_FILES			0x00008UL
#define OFFSET_FST_ENTRIES			0x0000CUL
#define OFFSET_TMD_HASH				0x001F4UL

/*
 * OTHER
 */
#define NB_CLUSTER_GROUP			64
#define NB_CLUSTER_SUBGROUP			8

class CWIIDisc{
public:
	BOOL ExtractPartitionFiles(struct image_file * image, u32 nPartition, u8 * cDirPathName);
	//BOOL DoPartitionShrink(struct image_file * image, u32 nPartition);
	//BOOL LoadDecryptedPartition(CString csName, struct image_file* image, u32 nPartition);
	BOOL SaveDecryptedPartition(const char* csName, struct image_file * image, u32 nPartition);
	//BOOL DoTheShuffle(struct image_file * image);
	//u64 GetFreePartitionStart(struct image_file *image);
	//u64 GetFreeSpaceAtEnd(struct image_file *image);
	//BOOL AddPartition(struct image_file * image, BOOL bChannel, u64 nOffset, u64 nDataSize, u8 * pText);
	//BOOL SetBootMode(struct image_file * image);
	_int64 nImageSize;

	//BOOL ResizePartition(struct image_file * image, u32 nPartition);
	//BOOL DeletePartition(struct image_file * image, u32 nPartition);
	//BOOL CheckForFreeSpace(struct image_file * image, u32 nPartition, u64 nOffset, u32 nBlocks);
	//u64 FindRequiredFreeSpaceInPartition(struct image_file * image, u64 nPartition, u32 nRequiredSize);
	//void aes_cbc_dec(u8 *in, u8 *out, u32 len, u8 *key, u8 *iv);
	//void aes_cbc_enc(u8 *in, u8 *out, u32 len, u8 *key, u8 *iv);
	//void sha1(u8 *data, u32 len, u8 *hash);

	//BOOL wii_write_data_file(struct image_file *iso, int partition, u64 offset, u64 size, u8 *in, FILE * fIn= NULL);
	/*int wii_write_clusters(struct image_file *iso, int partition, int cluster,  u8 *in, u32 nClusterOffset, u32 nBytesToWrite, FILE * fIn);
	int wii_read_data(struct image_file *iso, int partition, u64 offset, u32 size, u8 **out);
	int wii_read_cluster_hashes(struct image_file *iso, int partition, int cluster, u8 *h0, u8 *h1, u8 *h2);
	int wii_write_cluster(struct image_file *iso, int partition, int cluster, u8 *in);
	int wii_read_cluster(struct image_file *iso, int partition, int cluster, u8 *data, u8 *header);
	int wii_calc_group_hash(struct image_file *iso, int partition, int cluster);
	int wii_nb_cluster(struct image_file *iso, int partition);*/

	//BOOL wii_trucha_signing(struct image_file *image, int partition);
	//BOOL DiscWriteDirect(struct image_file * image, u64 nOffset, u8 * pData, unsigned int nSize);
	/*void MarkAsUnused(u64 nOffset, u64 nSize);*/
	//BOOL MergeAndRelocateFSTs(unsigned char *pFST1, u32 nSizeofFST1, unsigned char *pFST2, u32 nSizeofFST2, unsigned char *pNewFST,  u32 * nSizeofNewFST, u64 nNewOffset, u64 nOldOffset);
	//BOOL TruchaScrub(struct image_file * image, unsigned int nPartition);
	//BOOL RecreateOriginalFile(CString csScrubbedName, CString csDIFName, CString csOutName);
	BOOL CheckAndLoadKey(BOOL bLoadCrypto = FALSE, struct image_file *image = NULL);
	BOOL SaveDecryptedFile(const char* csDestinationFilename,  struct image_file *image,
							u32 part, u64 nFileOffset, u64 nFileSize, BOOL bOverrideEncrypt = FALSE);
	/*BOOL LoadDecryptedFile(CString csDestinationFilename,  struct image_file *image,
							u32 part, u64 nFileOffset, u64 nFileSize, int nFSTReference);*/
	void Reset(void);
	//void AddToLog(CString csText);
	//void AddToLog(CString csText, u64 nValue);
	//void AddToLog(CString csText, u64 nValue1, u64 nValue2);
	//void AddToLog(CString csText, u64 nValue1, u64 nValue2, u64 nValue3);
	void MarkAsUsed(u64 nOffset, u64 nSize);
	void MarkAsUsedDC(u64 nPartOffset, u64 nOffset, u64 nSize, BOOL bIsEncrypted);


	BOOL CWIIDisc::ExportFile(char* filename, char* targetPath, image_file *image, u32 nPartition, bool topLevelOnly);

	//BOOL CleanupISO(CString csFileIn, CString csFileOut, int nMode, int nHeaderMode = 0);
	unsigned int CountBlocksUsed();
	CWIIDisc();
	virtual ~CWIIDisc();

	unsigned char * pFreeTable;
	unsigned char * pBlankSector;
	unsigned char * pBlankSector0;

	//class CWIIScrubberDlg * m_pParent;

	//HTREEITEM	hPartition[20];
	//HTREEITEM	hDisc;

	u8						image_parse_header (struct part_header *header, u8 *buffer);
	struct image_file *		image_init (const char *filename);
	int						image_parse (struct image_file *image);
	void					image_deinit (struct image_file *image);
	u32						parse_fst (u8 *fst, const char *names, u32 i, struct tree *tree, struct image_file *image, u32 part);
	u8						get_partitions (struct image_file *image);
	void					tmd_load (struct image_file *image, u32 part);
	void					tmd_free (struct tmd *tmd);
	int						io_read (unsigned char  *ptr, size_t size, struct image_file *image, u64 offset);
	size_t					io_read_part (unsigned char  *ptr, size_t size, struct image_file *image, u32 part, u64 offset);
	int						decrypt_block (struct image_file *image, u32 part, u32 block);
	
	char*					pathToCommonKey;


//CString	m_csText;


private:
	u32 CWIIDisc::parse_fst_and_save(u8 *fst, const char *names, u32 i,
										struct image_file *image, u32 part);

	//u64 FindFirstData(u64 nStartOffset,  u64 nLength, BOOL bUsed = TRUE);
	//BOOL CopyDiscDataDirect(struct image_file * image, int nPart, u64 nSource, u64 nDest, u64 nLength);
	//u64 SearchBackwards(u64 nStartPosition, u64 nEndPosition);
	//void FindFreeSpaceInPartition(_int64 nPartOffset, u64 * pStart, u64 * pSize);
	//void Write32( u8 *p, u32 nVal);

	// save the tables instead of reading and writing them all the time
	u8 h3[SIZE_H3];
	u8 h4[SIZE_H4];
	u32 CWIIDisc::searchAndSaveFile(u8 *fst, const char *names, u32 i, struct image_file *image, u32 part, char* fileToSearch, char* targetPath, bool topLevelOnly);
	//CString m_csFilename;
};

#endif // !defined(AFX_WIIDISC_H__23AF31B0_FEBE_4B6D_ABA6_D778D6DA8683__INCLUDED_)


//class CWIIDisc
//{
//public:
//	BOOL CheckAndLoadKey(BOOL bLoadCrypto = FALSE, struct image_file *image = NULL);
//	BOOL SaveDecryptedFile(CString csDestinationFilename,  struct image_file *image,
//							u32 part, u64 nFileOffset, u64 nFileSize, BOOL bOverrideEncrypt = FALSE);
//	void MarkAsUsed(u64 nOffset, u64 nSize);
//	CWIIDisc(void);
//	int	io_read (unsigned char  *ptr, size_t size, struct image_file *image, u64 offset);
//	int	decrypt_block (struct image_file *image, u32 part, u32 block);
//};
