// stdafx.h : include file for standard system include files,
//  or project specific include files that are used frequently, but
//      are changed infrequently
//

#if !defined(AFX_STDAFX_H__2A07ABD0_3ED0_41C6_AC42_8A65D4E4825D__INCLUDED_)
#define AFX_STDAFX_H__2A07ABD0_3ED0_41C6_AC42_8A65D4E4825D__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000

#define VC_EXTRALEAN		// Exclude rarely-used stuff from Windows headers

//#include <afxwin.h>         // MFC core and standard components
//#include <afxext.h>         // MFC extensions
//#include <afxdisp.h>        // MFC Automation classes
//#include <afxdtctl.h>		// MFC support for Internet Explorer 4 Common Controls
#ifndef _AFX_NO_AFXCMN_SUPPORT
//#include <afxcmn.h>			// MFC support for Windows Common Controls
#endif // _AFX_NO_AFXCMN_SUPPORT
#ifndef FALSE
#define FALSE               0
#endif

#ifndef TRUE
#define TRUE                1
#endif

#include <sys/types.h>
#include <sys/stat.h>
#include "global.h"
#include <openssl/aes.h>

typedef int BOOL;
//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.


#endif // !defined(AFX_STDAFX_H__2A07ABD0_3ED0_41C6_AC42_8A65D4E4825D__INCLUDED_)
//enum tree_type {
//        TREE_RAW_FILE = 0,
//        TREE_FILE,
//        TREE_DIR,
//        TREE_MEM,
//        TREE_UINT,
//        TREE_HEX_UINT8,
//        TREE_HEX_UINT16,
//        TREE_HEX_UINT32,
//        TREE_HEX_UINT64,
//        TREE_CHAR,
//        TREE_STRING,
//        TREE_SYMLINK
//};

//struct tree {
//        u32 part;
///        enum tree_type type;
// /       char *name;
//        u64 v1;
//        u64 v2;
//        u16 nsubdirs;
//        struct tree *sub;
//        struct tree *next;
//};

enum tmd_sig {
        SIG_UNKNOWN = 0,
        SIG_RSA_2048,
        SIG_RSA_4096
};

struct tmd_content {
        u32 cid;
        u16 index;
        u16 type;
        u64 size;
        u8 hash[20];
};

struct tmd {
        enum tmd_sig sig_type; 
        u8 *sig;
        char issuer[64];
        u8 version;
        u8 ca_crl_version;
        u8 signer_crl_version;
        u64 sys_version;
        u64 title_id;
        u32 title_type;
        u16 group_id;
        u32 access_rights;
        u16 title_version;
        u16 num_contents;
        u16 boot_index;
        struct tmd_content *contents;
};

struct part_header {
        char console;
        u8 is_gc;
        u8 is_wii;

        char gamecode[2];
        char region;
        char publisher[2];

        u8 has_magic;
        char name[0x60];

        u64 dol_offset;
        u64 dol_size;

        u64 fst_offset;
        u64 fst_size;
};

enum partition_type {
        PART_UNKNOWN = 0,
        PART_DATA,
        PART_UPDATE,
        PART_INSTALLER,
        PART_VC
};

struct partition {
        u64 offset;

        struct part_header header;

        u64 appldr_size;

        u8 is_encrypted;

        u64 tmd_offset;
        u64 tmd_size;

        struct tmd * tmd;

		u64	h3_offset;

        char title_id_str[17];

        enum partition_type type;
        char chan_id[5];

        char key_c[35];
        AES_KEY key;

		u8 title_key[16];

        u64 data_offset;
        u64 data_size;

        u64 cert_offset;
        u64 cert_size;

        u8 dec_buffer[0x8000];

        u32 cached_block;
        u8 cache[0x7c00];
};

struct image_file {
 
		int fp;
//        void * mutex;

        u8 is_wii;

        u32 nparts;
        struct partition *parts;

 //       struct tree *tree;

        struct _stat st;

        u64 nfiles;
        u64 nbytes;

		u8	PartitionCount;
		u8	ChannelCount;

		u64 part_tbl_offset;
		u64	chan_tbl_offset;

        AES_KEY key;
};

#define WM_CANCELLED WM_USER+20
//// stdafx.h : include file for standard system include files,
//// or project specific include files that are used frequently,
//// but are changed infrequently
//
//#pragma once
//#include <sys/stat.h>
//#include "aes.h"
//
//typedef unsigned char u8;
//typedef unsigned short u16;
//typedef unsigned int u32;
//typedef unsigned __int64 u64;
//struct image_file {
// 
//		int fp;
////        void * mutex;
//
//        u8 is_wii;
//
//        u32 nparts;
//        struct partition *parts;
//
// //       struct tree *tree;
//
//        struct _stat st;
//
//        u64 nfiles;
//        u64 nbytes;
//
//		u8	PartitionCount;
//		u8	ChannelCount;
//
//		u64 part_tbl_offset;
//		u64	chan_tbl_offset;
//
//        AES_KEY key;
//};