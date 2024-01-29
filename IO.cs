namespace devector
{
    public class IO
    {
        private static Memory memory;
        private byte outport;
        private byte outbyte;
        private bool outset;

        IO()
        {
            this.outport = 0;
            this.outbyte = 0;
            outset = false;
        }

        public byte port_in(byte port)
        {
            /*
			byte result = 0xff;

			switch (port)
			{
				case 0x00:
					result = 0xff;
					break;
				case 0x01:
					
					// PC.low input ?
					auto pclow = (this->CW & 0x01) ? 0x0b : (this->PC & 0x0f);
					// PC.high input ?
					auto pcupp = (this->CW & 0x08) ?
						((this->tape_player.sample() << 4) |
							(this->keyboard.ss ? 0 : (1 << 5)) |
							(this->keyboard.us ? 0 : (1 << 6)) |
							(this->keyboard.rus ? 0 : (1 << 7))) : (this->PC & 0xf0);
					result = pclow | pcupp;
					
					break;
				case 0x02:
					if ((this->CW & 0x02) != 0)
					{
						result = this->keyboard.read(~this->PA); // input
					}
					else
					{
						result = this->PB;       // output
					}
					break;
				case 0x03:
					if ((this->CW & 0x10) == 0)
					{
						result = this->PA;       // output
					}
					else
					{
						result = 0xff;          // input
					}
					break;

				case 0x04:
					result = this->CW2;
					break;
				case 0x05:
					result = this->PC2;
					break;
				case 0x06:
					result = this->PB2;
					break;
				case 0x07:
					result = this->PA2;
					break;

				// Timer
				case 0x08:
				case 0x09:
				case 0x0a:
				case 0x0b:
					return this->timer.read(~(port & 3));

				// Joystick "C"
				case 0x0e:
					return this->joy_0e;
				case 0x0f:
					return this->joy_0f;

				case 0x14:
				case 0x15:
					result = this->ay.read(port & 1);
					break;

				case 0x18: // fdc data
					result = this->fdc.read(3);
					break;
				case 0x19: // fdc sector
					result = this->fdc.read(2);
					break;
				case 0x1a: // fdc track
					result = this->fdc.read(1);
					break;
				case 0x1b: // fdc status
					result = this->fdc.read(0);
					break;
				case 0x1c: // fdc control - readonly
						   //result = this->fdc.read(4);
					break;
				default:
					break;
			}
			/*
			 * a callback for debuging
			 * TODO: clean it
			if (this->onread)
			{
				int hookresult = this->onread((uint32_t)port, (uint8_t)result);
				if (hookresult != -1)
				{
					result = hookresult;
				}
			}

			return result;
			*/
            return 0;
        }

        public void port_out(byte port, byte value)
        {
            /*
			 * a callback for debugging
			 * TODO: clean it
            if (this->onwrite)
            {
                this->onwrite((uint32_t)port, (uint8_t)w8);
            }
			*/
            this.outport = port;
            this.outbyte = value;
        }

        public IO(Memory _memory)
        {
            memory = _memory;
        }
    }
}
