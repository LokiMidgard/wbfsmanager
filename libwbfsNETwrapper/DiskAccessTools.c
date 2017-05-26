#include "DiskAccessTools.h"
int __stdcall read_wii_disc_sector(void *_handle, u32 _offset, u32 count, void *buf)
{
	HANDLE *handle = (HANDLE *)_handle;
	LARGE_INTEGER large;
	DWORD read;
	u64 offset = _offset;
	
	offset <<= 2;
	large.QuadPart = offset;
	
	if (SetFilePointerEx(handle, large, NULL, FILE_BEGIN) == FALSE)
	{
		wbfs_error("error seeking in disc file");
		return 1;
	}
	
	read = 0;
	if((ReadFile(handle, buf, count, &read, NULL) == FALSE) || !read)
	{
		wbfs_error("error reading wii disc sector");
		return 1;
	}

	if(read < count)
	{
		wbfs_warning("warning: requested %d, but read only %d bytes (trimmed or bad padded ISO)", count, read);
		wbfs_memset((u8*)buf+read, 0, count-read);
	}

	return 0;
}

int __stdcall write_wii_disc_sector(void *_handle, u32 lba, u32 count, void *buf)
{
	HANDLE *handle = (HANDLE *)_handle;
	LARGE_INTEGER large;
	DWORD written;
	u64 offset = lba;
	
	offset *= 0x8000;
	large.QuadPart = offset;
	
	if (SetFilePointerEx(handle, large, NULL, FILE_BEGIN) == FALSE)
	{
		//fprintf(stderr,"\n\n%lld %p\n", offset, handle);
		wbfs_error("error seeking in wii disc sector (write)");
		return 1;
	}
	
	written = 0;
	if (WriteFile(handle, buf, count * 0x8000, &written, NULL) == FALSE)
	{
		wbfs_error("error writing wii disc sector");
		return 1;
	}

	if (written != count * 0x8000)
	{
		wbfs_error("error writing wii disc sector (size mismatch)");
		return 1;
	}
	
	return 0;
}

HANDLE* CreateFileHP(
    __in     LPCSTR lpFileName,
    __in     DWORD dwDesiredAccess,
    __in     DWORD dwShareMode,
    __in_opt LPSECURITY_ATTRIBUTES lpSecurityAttributes,
    __in     DWORD dwCreationDisposition,
    __in     DWORD dwFlagsAndAttributes,
    __in_opt HANDLE hTemplateFile
    )
{
	HANDLE *handle = CreateFile( lpFileName, dwDesiredAccess,dwShareMode,lpSecurityAttributes,dwCreationDisposition,dwFlagsAndAttributes,hTemplateFile);
	return handle;
}