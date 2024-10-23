#pragma once
#include <string>
#include <windows.h>
#include <iostream>

extern "C" __declspec(dllimport) int MyExportedFunction44();

typedef int(__cdecl* MyExportedFunctionType)();

namespace dev 
{
    public ref class HAL
    {
    public:

        int field1;
        float field2;

        // A constructor that sets initial values
        HAL(int f1, float f2) {
            field1 = f1;
            field2 = f2;
        }

        // A method to demonstrate interop
        void DisplayData() {
            System::Console::WriteLine("Field1: {0}, Field2: {1}", field1, field2);
        }

        static int DisplayData2() {

            int prnt = MyExportedFunction44();
            System::Console::WriteLine("MyExportedFunction44 !!!: {0}", prnt);
/*

            const wchar_t* dllName = L"HAL.dll";  // Change this to your actual DLL name

            // Print full directory path before loading the DLL
            auto dllpath = GetDLLPath(dllName);

            // Get the directory of the current DLL (HAL.dll)
            wchar_t fullPath[MAX_PATH];
            DWORD result1 = GetFullPathName(dllName, MAX_PATH, fullPath, nullptr);

            if (result1 > 0 && result1 < MAX_PATH) {
                // Extract the directory from the full path
                wchar_t* lastSlash = wcsrchr(fullPath, L'\\');
                if (lastSlash != nullptr) {
                    *lastSlash = L'\0'; // Null-terminate the string at the last slash

                    // Add the directory to the DLL search path
                    SetDllDirectory(fullPath);

                    System::String^ dir = gcnew System::String(fullPath, 0, wcslen(fullPath));
                    System::Console::WriteLine("Full dir to the DLL: {0}", dir);
                }
            }
            else {
                System::Console::WriteLine("Failed to retrieve full path for DLL");
                return -1;
            }

            // Load the DLL manually
            HMODULE hDll = LoadLibrary(dllName);
            if (hDll == NULL) {
                System::Console::WriteLine("Failed to load the DLL!");
                return -1;
            }

            // Get the function address
            MyExportedFunctionType MyExportedFunction = (MyExportedFunctionType)GetProcAddress(hDll, "MyExportedFunction44");
            if (MyExportedFunction == NULL) {
                System::Console::WriteLine("Failed to get the function address!");
                FreeLibrary(hDll);
                return -1;
            }

            // Call the function
            int result = MyExportedFunction();
            System::Console::WriteLine("Result from MyExportedFunction44: {0}", result);

            // Free the DLL
            FreeLibrary(hDll);
*/
            return 0;

        }

        static std::wstring GetDLLPath(const wchar_t* dllName)
        {
            wchar_t fullPath[MAX_PATH];

            // Get the full path of the DLL file
            DWORD result = GetFullPathName(dllName, MAX_PATH, fullPath, nullptr);

            if (result > 0 && result < MAX_PATH) {
                System::String^ managedFullPath = gcnew System::String(fullPath, 0, wcslen(fullPath));
                System::Console::WriteLine("Full path to the DLL: {0}", managedFullPath);
            }
            else {
                System::String^ managedDllName = gcnew System::String(dllName, 0, wcslen(dllName));
                System::Console::WriteLine("Failed to retrieve full path for DLL: {0}", managedDllName);
            }

            return result > 0 && result < MAX_PATH ? fullPath : std::wstring{};
        }
    };
}

