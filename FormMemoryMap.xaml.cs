using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Media;

namespace devector
{
    /// <summary>
    /// Interaction logic for FormMemoryMap.xaml
    /// </summary>
    public partial class FormMemoryMap : Window
    {
        private const int BITS_IN_BYTE = 8;

        private const int BLOCKS_W = 4;
        private const int BLOCKS_H = 2 * 5;
        private const int BLOCK_PXL_SIZE = 256;
        private const int BLOCK_BYTES_W = BLOCK_PXL_SIZE / BITS_IN_BYTE;
        private const int BLOCK_BYTES_H = BLOCK_PXL_SIZE;
        private const int BLOCK_LEN = BLOCK_BYTES_W * BLOCK_BYTES_H;

        private const int MAP_W = BLOCK_PXL_SIZE * BLOCKS_W;
        private const int MAP_H = BLOCK_PXL_SIZE * BLOCKS_H;
        private UInt32 fill_color;

        public WriteableBitmap map;
        public static UInt32[] map_data = new UInt32[MAP_W * MAP_H];
        protected GCHandle data_handle { get; private set; }
        /*
        UInt32 color_bit_on01 = (UInt32)(Color.Bisque.ToArgb());
        UInt32 color_bit_on02 = (UInt32)(Color.Beige.ToArgb());
        UInt32 color_bit_off0 = (UInt32)(Color.Black.ToArgb());
        UInt32 color_bit_off1 = (UInt32)(Color.FromArgb(20, 20, 30).ToArgb());
        UInt32 color_bit_off_offset_bank_64k = (UInt32)(Color.FromArgb(15, 15, 15).ToArgb());// make every second 64 KB block a bit brighter to visually split up the ram and the ram-disk banks
        */
        readonly UInt32 color_bit_on01 = color_to_argb(0xff, 0xe4, 0xc4); // Bisque
        readonly UInt32 color_bit_on02 = color_to_argb(0xf5, 0xf5, 0xdc); // Beige
        readonly UInt32 color_bit_off0 = color_to_argb(0x00, 0x00, 0x00); // Black
        readonly UInt32 color_bit_off1 = color_to_argb(20, 20, 20);
        readonly UInt32 color_bit_off_offset_bank_64k = color_to_argb(15, 15, 15);

        static public UInt32 color_to_argb(byte r, byte g, byte b, int alpha = 0xff)
        {
            return (UInt32)(alpha<<24 | (r << 16) | (g << 8) | b);
        }

        public FormMemoryMap()
        {
            InitializeComponent();
            init();
        }

        void init()
        {
            data_handle = GCHandle.Alloc(map_data, GCHandleType.Pinned);
            map = new WriteableBitmap(MAP_W, MAP_H, 96, 96, PixelFormats.Bgra32, null);
            Array.Fill(map_data, 0xff000000);

            picturebox_map.Source = map;

            draw_map();
        }

        private void update_image(int x = 0, int y = 0, int width = MAP_W, int height = MAP_H)
        {
            var rect = new Int32Rect(x, y, width, height);
            map.Lock();
            map.WritePixels(rect,
                data_handle.AddrOfPinnedObject(),
                width * height * sizeof(UInt32),
                width * sizeof(UInt32));
            map.Unlock();
        }

        private void draw_map()
        {
            int blockWidth = BLOCK_PXL_SIZE;
            int blockHeight = BLOCK_PXL_SIZE;

            for (int block_vert_idx = 0; block_vert_idx < BLOCKS_H; block_vert_idx++)
            {
                for (int block_horiz_idx = 0; block_horiz_idx < BLOCKS_W; block_horiz_idx++)
                {
                    int blockX = block_horiz_idx * blockWidth;
                    int blockY = block_vert_idx * blockHeight;

                    var block_idx = block_vert_idx * BLOCKS_W + block_horiz_idx;
                    var every_second_block = ((block_horiz_idx % 2) + (block_vert_idx % 2)) % 2 == 0;
                    var color_bit_off = every_second_block ? color_bit_off0 : color_bit_off1;

                    color_bit_off += ((block_idx / 4) & 2) == 0 ? color_bit_off_offset_bank_64k : 0; // make every second 64 KB block a bit brighter to visually split up the ram and the ram-disk banks

                    var color_bit_on = every_second_block ? color_bit_on01 : color_bit_on02;

                    draw_memory_block(block_vert_idx * BLOCKS_W + block_horiz_idx, blockX, blockY, color_bit_on, color_bit_off);
                }
            }
            update_image();
        }
        // draw a 8 KB block
        private void draw_memory_block(int blockIndex, int x, int y, UInt32 color_bit_on, UInt32 color_bit_off)
        {
            for (int byte_vert_idx = 0; byte_vert_idx < BLOCK_BYTES_H; byte_vert_idx++)
            {
                for (int byte_horix_idx = 0; byte_horix_idx < BLOCK_BYTES_W; byte_horix_idx++)
                {
                    uint addr = (uint)(blockIndex * BLOCK_LEN + byte_horix_idx * BLOCK_PXL_SIZE + byte_vert_idx);
                    byte memoryByte = Hardware.memory.get_byte(addr, Memory.AddrSpace.GLOBAL);

                    draw_byte(memoryByte, x + byte_horix_idx * BITS_IN_BYTE, y + BLOCK_PXL_SIZE - 1 - byte_vert_idx, color_bit_on, color_bit_off);
                }
            }
        }

        private void draw_byte(byte memoryByte, int x, int y, UInt32 color_bit_on, UInt32 color_bit_off)
        {
            for (int bit = 0; bit < BITS_IN_BYTE; bit++)
            {
                bool isSet = (memoryByte & (1 << bit)) != 0;
                fill_color = isSet ? color_bit_on : color_bit_off;
                map_data[(x + BITS_IN_BYTE - 1 - bit) + y * MAP_W] = fill_color;
            }
        }
    }
}
