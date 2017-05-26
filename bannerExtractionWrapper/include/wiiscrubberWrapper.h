// wiiscrubberWrapper.h

#pragma once
#include "stdafx.h"
#include "WIIDisc.h"
extern "C"
{
__declspec(dllexport) int ExtractOpeningBnr(char* isoFileName, char* pathToKey, char* targetFilename);
}