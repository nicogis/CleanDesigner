# Clean Designer XPO DevExpress XAF

## Overview

**CleanDesigner** is a .NET 8 utility designed to help developers working with DevExpress XAF/XPO projects. When generating business classes using the XAF XPO wizard, the tool analyzes and cleans up `.Designer.cs` files by comparing them to their corresponding custom class files. Its goal is to identify and optionally remove duplicate properties and unnecessary backing fields that may be introduced when resynchronizing the model from the database.

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

#### Report designer files (e.g., 'b')
```CleanDesigner -path "C:\Your\XAF\Project\BusinessObjects\ORMDataModel1Code" -report -prefix b```

#### Clean designer files with a custom backing field prefix (using default prefix 'f')
```CleanDesigner -path "C:\Your\XAF\Project\BusinessObjects\ORMDataModel1Code" -clean```
