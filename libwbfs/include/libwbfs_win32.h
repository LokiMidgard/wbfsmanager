#ifndef LIBWBFS_WIN32_H
#define LIBWBFS_WIN32_H

#ifdef WIN32

#include <windows.h>
#include <setupapi.h>
#include <stdio.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <string.h>
#include <fcntl.h>

#include "libwbfs.h"
static int __stdcall read_sector(void *_handle, u32 lba, u32 count, void *buf);
static int __stdcall write_sector(void *_handle, u32 lba, u32 count, void *buf);
static void __stdcall close_handle(void *handle);
static int get_capacity(char *fileName, u32 *sector_size, u32 *sector_count);
wbfs_t *wbfs_try_open_hd(char *driveName, int reset);
wbfs_t *wbfs_try_open_partition(char *partitionLetter, int reset);
wbfs_t* wbfs_try_open(char *disc, char *partition, int reset);
#endif
#endif