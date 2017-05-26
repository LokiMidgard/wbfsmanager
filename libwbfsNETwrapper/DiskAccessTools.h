#ifndef _DISKACCESSTOOLS_H
#define _DISKACCESSTOOLS_H

#include <windows.h>
#include <setupapi.h>
#include <stdio.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <string.h>
#include <fcntl.h>

#include "CSWtools.h"
#include "libwbfs_os.h"
int __stdcall read_wii_disc_sector(void *_handle, unsigned int _offset, unsigned int count, void *buf);
int __stdcall write_wii_disc_sector(void *_handle, unsigned int lba, unsigned int count, void *buf);
HANDLE* CreateFileHP(
    __in     LPCSTR lpFileName,
    __in     DWORD dwDesiredAccess,
    __in     DWORD dwShareMode,
    __in_opt LPSECURITY_ATTRIBUTES lpSecurityAttributes,
    __in     DWORD dwCreationDisposition,
    __in     DWORD dwFlagsAndAttributes,
    __in_opt HANDLE hTemplateFile
    );

#endif