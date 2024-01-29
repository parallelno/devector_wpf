using System;
using System.IO;

namespace devector
{
    public class Hardware
    {

        public static I8080 cpu;
        public static Memory memory;
        public static IO io;
        public static Display display;
        public static Debugger debugger;

        public Hardware()
        {
            memory = new Memory();
            io = new IO(memory);
            debugger = new Debugger(memory);
            cpu = new I8080(memory.get_byte, memory.set_byte, io.port_in, io.port_out, debugger.mem_access);
            display = new Display(memory);
        }

        public void load_rom(string path)
        {
            byte[] file_data;

            try
            {
                file_data = File.ReadAllBytes(path);

                if (file_data != null && file_data.Length > 0 && file_data.Length <= Memory.MEMORY_MAIN_LEN)
                {
                    init();
                    memory.load(file_data);
                    Console.WriteLine("file loaded: " + path);
                }
            }
            catch (DirectoryNotFoundException dirEx)
            {
                Console.WriteLine("Directory not found: " + dirEx.Message);
            }
        }

        private void init()
        {
            memory.init();
            cpu.init();
            display.init();
            debugger.init();
        }

        // rasterizes the frame. For realtime emulation it should be called by the 50.08 Hz (3000000/59904) timer
        public static void execute_frame()
        {
            do
            {
                execute_instruction();
            } while (!display.T50HZ);
        }

        public static void execute_instruction()
        {
            do
            {
                display.rasterize();
                cpu.execute_machine_cycle(display.T50HZ);
            } while (cpu.machine_cycle != I8080.INSTR_EXECUTED);
        }
    }
}
