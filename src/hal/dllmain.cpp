// dllmain.cpp : Defines the entry point for the DLL application.
//#include "pch.h"
//#include "hal.h"

#define WIN32_LEAN_AND_MEAN             // Exclude rarely-used stuff from Windows headers
// Windows Header Files
#include <windows.h>

#include <string>


volatile int t = 1;

extern "C" __declspec(dllexport) std::wstring MyExportedFunction44(const std::wstring& _str) 
{
    int t = 1;
    return _str + std::to_wstring(t);
}

BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}