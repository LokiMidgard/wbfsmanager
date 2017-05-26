#ifndef LIBWBFSEXT_H
#define LIBWBFSEXT_H

#include "libwbfs.h"
#include "libwbfs_win32.h"

#define ERR(x) do {wbfs_error(x);goto error;}while(0)
//-10 : Debugging exception, the n_wbfs_sec_per_disc were not equal!
//-11 : wbfs_sec_sz was not equal!
int wbfs_driveToDriveCopy
	(
		wbfs_t *srcPartitionPtr,
		wbfs_t *targetPartitionPtr,
		wbfs_disc_t* discPtr,		//Src disc
		progress_callback_t progressCallback
		//partition_selector_t selector
	);

#endif