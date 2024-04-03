# Additions to the solution for running the project in **VSCode** and running xUnit tests

## XUnit testing using VScode

Open an integrated terminal.  
    *Right-click on an item in the solution and click 'open integrated terminal'
    * you may need to  type `cd ..` to get to the correct level.  
    * Verify the folder being used contains the .sln file by using `ls` to list the folder contents
Create a template for the unit test by entering the following command in the terminal at the solution level:  
    `dotnet new xunit -o XPlaneConnector.Tests`

Next add a reference to the Main Part of the solution Testing Portion's project file XPlaneConnector.Test.csproj
    `dotnet add ./XPlaneConnector.Tests/XPlaneConnector.Tests.csproj reference ./XPlaneConnector/XPlaneConnector.csproj`

The third step is to update to .sln file.  Run the following command:
    `dotnet sln add ./XPlaneConnector.Tests/XPlaneConnector.Tests.csproj`

Update the code in XPlaneConnectors.Tests to actually create the tests.  Here is some example code for running a simple unit test:

```C#
using Xunit;
using XPlaneConnector;

namespace XPlaneNexus.UnitTests
{
    public class XPlaneConnector_IsInstanceRunning
    {
        [Fact]
        //TODO ========== Make this a real unit test!
        public void IsPrime_InputIs1_ReturnFalse()
        {
            var primeService = new PrimeService();
            bool result = primeService.IsPrime(1);

            Assert.False(result, "1 should not be prime");
        }
    }
}
```

Now in the integrated terminal run
    `dotnet test`

Tests with additional parameters can be run using Theory as shown in the example below.  This would be in the UnitTest.cs file

```c#
[Theory]
[InlineData(-1)]
[InlineData(0)]
[InlineData(1)]
public void IsPrime_ValuesLessThan2_ReturnFalse(int value)
{
    var result = _primeService.IsPrime(value);

    Assert.False(result, $"{value} should not be prime");
}
```

## Adding another project to the solution

The existing solution on Github that was forked on March 12, 2024 lacks a method for discovering instances of XPlane
Add a new class to the project using the following command.  Note that this will also create a new .csproj file
    `dotnet new classlib -o XPlaneDiscovery`

This class also needs to be referenced by the testing class, so a reference is needed.  
    `dotnet add ./XPlaneConnector.Tests/XPlaneConnector.Tests.csproj reference ./XPlaneDiscovery/XPlaneDiscovery.csproj`

The new class must be added to the solutions file so it is included in any builds
    `dotnet sln XPlaneConnector.sln add XPlaneDiscovery/XPlaneDiscovery.csproj`
    (or right-click in the solution explorer and add existing project)

## Modern approaches to the build files

A modern approach to the bin, obj and other build artifacts is to put all those into a single artifacts file and then use the github exclude.  
Create a ##directory.build.props** file with the following contents.  The key line is "\<UseArtifactsOutput\>true\</UseArtifactsOutput\>"

  ```c#
  <Project>
  <PropertyGroup>
    <Platforms>AnyCPU;x86;x64</Platforms>
    <TargetFrameworks>net8.0</TargetFrameworks>

    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseArtifactsOutput>true</UseArtifactsOutput>

    <IsPackable>false</IsPackable>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>

  <ItemGroup>
    <AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleTo">
      <_Parameter1>$(MSBuildProjectName).Tests</_Parameter1>
    </AssemblyAttribute>
  </ItemGroup>
</Project>
  ```

## Enabling building, and testing in VSCode

Without configuring anything ...

* Pressed run and debug...
  * **Select debugger** dialog box popped up,
    * Selected **C#** from the dropdown list
  * **Select Launch configuration** dialog pop-up,
    * selected XPlaneConnector.Tests from the dropdown list

Now in the run and debug I see a C#:XplaneXConnector.Tests option in the dropdown list

Unlike Visual Code Studio, VSCode requires the developer to hand-craft the files needed for building and testing the program.  These files are stored in a .vscode file at the solution-level of the projects.  Rather than running `dotnet test` in the terminal, a command can be
configured in the launch.json so that pressing the F5 key can be used to start the test.  Rather than duplicating the json file here, review it in the .vscode folder
