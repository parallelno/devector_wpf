# Devector

## Overview

This project focuses on transitioning the Devector emulator from pure C++ to a c++ emulation back-end and a C# front-end with WPF UI api.
Devector is a multi-platform emulator of a Soviet personal computer Vector06c. It is designed to simplify the development process and speed up the work. Currently, it is in the early stages of development, so please use it on your own risk.
This particular project is for Windows platform only now!

## Features

- A multi-platform precise emulator of a Soviet personal computer Vector06c
- Debugging functionality
- FDD support
- Up to 8 Ram-Disk support
- AY & bipper & 3-channel timer support
- Recording a playback with options to store, load, and play it

## Usage

On Windows
1. Run the emulator: `Devector.exe`

## Build

On Windows
It requires VS 2022+ with workloads: .NET desktop development, Desktop development with C++, Individual components: C++/CLI Support for build tools (latest)
1. open Visual Studio solution and build win32 or x64 app
2. copy the content of the resources folder into the bin folder

## Contributing

Contributions are welcome! If you find a bug or have an idea for a feature, please create an issue or submit a pull request.

## License

Devector is licensed under the MIT License. See the LICENSE file for more information.

## Acknowledgements

Special thanks to the following people for their contributions:

- [Svofski](https://github.com/svofski)
- [Viktor Pykhonin](https://github.com/vpyk/)
- [Yuri Larin](https://github.com/ImproverX)
- [zx-pk.ru comunity](https://zx-pk.ru/)
