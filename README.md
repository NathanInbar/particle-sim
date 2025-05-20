# ParticleSim - 2D Simulation Framework

A 2D simulation framework built with C# and MonoGame.

## Current Features

* Renders a 2D scene with a black background.
* Implements a basic 2D camera for viewing the world space.
* Spawns simple, square-shaped particles from the center of the screen.
* Particles have random initial velocities, colors, sizes, and lifetimes.
* Particles move linearly and despawn after their lifetime expires.
* Basic particle object pooling is in place to manage a maximum number of particles.

## Environment Setup

To build and run this project, you'll need the following:

1.  **.NET SDK:**
    * This project targets **.NET 8** (or the .NET version specified in the `.csproj` file).
    * Download and install the .NET SDK from [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download).
    * Verify installation by opening a terminal and typing `dotnet --version`.

2.  **MonoGame Templates:**
    * Install the MonoGame C# project templates by running the following command in your terminal:
        ```bash
        dotnet new install MonoGame.Templates.CSharp
        ```

3.  **Visual Studio Code (Recommended Editor):**
    * Download from [code.visualstudio.com](https://code.visualstudio.com/).
    * **Required Extension:** Install the "C# Dev Kit" extension (published by Microsoft) from the VS Code Marketplace. This will provide C# language support, debugging, and project management.

## Getting Started: Build & Run

1.  **Clone the Repository (if applicable):**
    * If you're accessing this from a Git repository, clone it to your local machine:
        ```bash
        git clone <repository-url>
        cd particle-sim
        ```

2.  **Navigate to the Project Directory:**
    * Open your terminal/command prompt and navigate to the root of the `particle-sim` project folder (the one containing the `.csproj` file).

3.  **Restore .NET Local Tools:**
    * MonoGame uses the MGCB Editor for content management, which is registered as a .NET local tool. Restore it by running:
        ```bash
        dotnet tool restore
        ```

4.  **Build the Project:**
    * Compile the C# code using the .NET CLI:
        ```bash
        dotnet build
        ```

5.  **Run the Project:**
    * **Using the .NET CLI:**
        ```bash
        dotnet run
        ```
    * **Using VS Code (Recommended for Debugging):**
        * Open the `particle-sim` folder in VS Code (`File > Open Folder...`).
        * The C# Dev Kit extension should automatically detect the project and configure a launch profile.
        * Go to the "Run and Debug" view (Ctrl+Shift+D or the play icon in the sidebar).
        * Select the appropriate launch configuration (usually named something like ".NET Core Launch (console)" or after your project).
        * Press **F5** or click the green play button to build and run the application with the debugger attached.

## Testing the Current Features

Once the application is running, you should observe the following:

* A window will appear with a **black background**.
* Colored **square particles** will continuously spawn from the center of the screen.
* These particles will move outwards in random directions.
* Particles will disappear after their individual lifetimes expire.

**Controls:**

* **`Esc`:** Press the Escape key to close the application window.
* **`Left`:** Pan Camera Left
* **`Up`:** Pan Camera Up
* **`Right`:** Pan Camera Right
* **`Down`:** Pan Camera Down


## Managing Content

Game assets (textures, fonts, shaders, etc.) are managed through the MonoGame Content Builder (MGCB) Editor.
* The content project file is located at `Content/Content.mgcb`.
* To open the MGCB Editor, navigate to your project's root directory in the terminal and run:
    ```bash
    dotnet mgcb-editor
    ```
    *(Currently, the only "texture" is a 1x1 white pixel generated programmatically in `LoadContent()` for drawing particles, so no assets need to be manually added to `Content.mgcb` for the current features.)*
