#include <string.h>
#include <stdlib.h>
#include <direct.h>		//chdir used in unpackWad

extern "C"
{
	#include "tools.h"
	__declspec(dllexport) int packWad(char* pathToOpeningBnr, char* pathToTicket, char* pathToTMD, char* pathToCertFile, char* pathToCommonKey, char* newTitleID, char* workingPath, char* targetPath);
	__declspec(dllexport) int unpackWad(char* pathToWad, char* pathToCommonKey, char* certFilename, char* tikFilename, char* tmdFilename, char* trailerFilename, char* outputFolder);
	
	int unpackWadInternal(u8* header, char* outputFolder, char* pathToCommonKey, char* certFilename, char* tikFilename, char* tmdFilename, char* trailerFilename);
	u8* get_wad(u32 len);
	int get_appfile(u8 *app, u32 app_len, u8 *tik, u8 *tmd, char* pathToCommonKey);
}
