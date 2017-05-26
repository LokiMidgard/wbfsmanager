#pragma once
#include <string>

using namespace std;
extern "C"
{
	#include "libwbfs.h"
	#include "libwbfs_win32.h"
	//#include "CSWtools.h"

	enum RegionCode { NTSC, NTSCJ, PAL, KOR, NOREGION };

	typedef void (__stdcall*fatal_error_callback_t)(const char* message);

	__declspec(dllexport) bool OpenDrive(char* partitionLetter);
	
	__declspec(dllexport) void CloseDrive();

	__declspec(dllexport) bool FormatDrive(char* partitionLetter);

	__declspec(dllexport) int GetDiscCount();

	__declspec(dllexport) int GetDiscInfo(int index, char* discId, float* size, char* discName);

	__declspec(dllexport) int GetDiscInfoEx(int index, char* discId, float* sizeGB, char* discName, RegionCode* regionCode);

	__declspec(dllexport) int GetUsedBlocksCount();

	__declspec(dllexport) int GetDiscImageInfo(const char* filename, char* discId, float* estimatedSize, char* discName, partition_selector_t partitionSelection);

	__declspec(dllexport) int GetDiscImageInfoEx(const char* filename, char* discId, float* estimatedSize, char* discName, RegionCode* regionCode, partition_selector_t partitionSelection);
	
	__declspec(dllexport) int AddDiscToDrive(char* filename, progress_callback_t progressCallback, partition_selector_t wiiPartitionToAdd, bool copy1to1, char *newName);

	__declspec(dllexport) int RemoveDiscFromDrive(char* discId);

	__declspec(dllexport) int ExtractDiscFromDrive(char* discId, progress_callback_t progressCallback, char* targetName);

	__declspec(dllexport) int RenameDiscOnDrive(char* discId, char* newName);

	__declspec(dllexport) void SubscribeErrorEvent(fatal_error_callback_t errorEventHandler);

	__declspec(dllexport) int GetDriveStats(unsigned int* blocks, float* total, float* used, float* free);

	__declspec(dllexport) int DriveToDriveSingleCopy(char* targetPartitionLetter, progress_callback_t progressCallback/*, partition_selector_t wiiPartitionToAdd*/, char* discId);

	__declspec(dllexport) int CanDoDirectDriveToDrive(char* targetPartitionLetter);

	void fatal(const char *s, ...);
	void non_fatal(const char *s, ...);
	int CustomStrLenU(unsigned char* data);
	int CustomStrLen(char* data);
	int __stdcall read_wii_disc_sector(void *_handle, u32 _offset, u32 count, void *buf);
	int __stdcall write_wii_disc_sector(void *_handle, u32 lba, u32 count, void *buf);
}