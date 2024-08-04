# SubjectLift

SubjectLift is a desktop application built using Avalonia UI and C#. It allows to segment objects in your images like you can do in iPhone.  This project is an exercise in using [Segment Anything Model](https://github.com/facebookresearch/segment-anything) and ONNX with C#. There are a lot of improvements that can be made in code performance, segmentation and UX. This project represents a reference for using a model ported to ONNX with C#.

The inference is very slow and runs on the CPU. It can be made to run in the GPU but you will need to install additional software from NVIDIA so I skipped it.

## Features

- **Open Image**: Load an image into the application using Ctrl+O.
- **Segment Subject**: Click on a subject within the image to get the segmented mask and display it.

### Screenshots

##### Source Image
![Original](https://github.com/user-attachments/assets/aa91efda-c118-45fc-945e-3ed509e1c84b)



##### Generated Mask
![OriginalWithMask](https://github.com/user-attachments/assets/636fc7c0-0541-4323-b2ef-c9d9eb3a1e39)



##### Mask applied to source
![MaskedResult](https://github.com/user-attachments/assets/046eaeff-57a4-4a87-96f3-077299ae1e7c)



##### Final UI
![FinalResult](https://github.com/user-attachments/assets/a6d58a9f-6162-4858-9e60-98f83f97aff0)



## Technologies Used

- **[Avalonia UI](https://github.com/AvaloniaUI/Avalonia)**
- **[Segment Anything Model](https://github.com/facebookresearch/segment-anything)**
- **[ONNX Runtime for C#](https://onnxruntime.ai/docs/get-started/with-csharp.html)**

## Getting Started

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download)
- [JetBrains Rider](https://www.jetbrains.com/rider/) or any other C# IDE

### Installation

1. Clone the repository
4. Download the encoder and decoder models from [this link](https://huggingface.co/visheratin/segment-anything-vit-b/tree/main) and place them in the `Models` folder where the executable is located. Usually `SubjectLift.Desktop\bin\Debug\net8.0\Models`

### Running the Application

1. Open the project in your IDE.
2. Build the project.
3. Run the application.

## Project Structure

- `App.axaml.cs`: Entry point of the application.
- `Views/MainWindow.axaml`: XAML layout for the main window.
- `Views/MainWindow.axaml.cs`: Code-behind for the main window.
- `Controls/MaskedImage.cs`: Custom control for displaying images with masks.

## Contributing

Contributions are welcome! Please open an issue or submit a pull request for any improvements or bug fixes.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
