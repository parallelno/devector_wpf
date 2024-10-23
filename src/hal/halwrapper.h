#pragma once
#include <string>
#include <windows.h>
#include <iostream>

extern "C" __declspec(dllimport) std::wstring MyExportedFunction44(const std::wstring& str);

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

            auto prnt = MyExportedFunction44(L"something ");

            System::String^ bstr = gcnew System::String(prnt.c_str(), 0, prnt.size());
            System::Console::WriteLine("MyExportedFunction44 !!!: {0}", bstr);

            return 0;
        }
    };
}

