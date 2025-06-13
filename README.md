# Clean Designer XPO DevExpress XAF

## Overview

**CleanDesigner** is a .NET 8 utility designed to help developers working with DevExpress XAF/XPO projects. When generating business classes using the XAF XPO wizard, the tool analyzes and cleans up `.Designer.cs` files by comparing them to their corresponding custom class files. Its main goal is to identify and optionally remove duplicate properties and unnecessary backing fields that may be introduced during the code generation process.

## Features

- **Report Mode:** Lists duplicate properties and associated backing fields found in `.Designer.cs` files compared to the custom class files.
- **Clean Mode:** Removes duplicate properties and unnecessary backing fields from `.Designer.cs` files, keeping your codebase clean and maintainable.
- **Customizable Backing Field Prefix:** Supports custom prefixes for backing fields (default: `f`).

## Usage

### Command Line Arguments

- `-path <directory>`  
  Path to the directory containing the `.Designer.cs` and custom class files. **(Required)**
- `-prefix <char>`  
  Prefix for backing fields (default: `f`). **(Optional)**
- `-clean`  
  Clean designer files by removing duplicates and unnecessary fields. **(Mutually exclusive with `-report`)**
- `-report`  
  Only report duplicates and backing fields without modifying files. **(Mutually exclusive with `-clean`)**

> **Note:** You must specify either `-clean` or `-report`, but not both.

### Example

#### Report designer files (using default prefix 'f')

```dotnet run -- -path "C:\Your\XAF\Project\BusinessObjects\ORMDataModel1Code" -report```

#### Clean designer files with a custom backing field prefix (e.g., 'b')

```dotnet run -- -path "C:\Your\XAF\Project\BusinessObjects\ORMDataModel1Code" -prefix b -clean```
