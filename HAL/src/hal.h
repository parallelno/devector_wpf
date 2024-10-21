#pragma once


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

        static void DisplayData2() {
            System::Console::WriteLine("Field1: 10, Field2: 11");
        }
    };
}

