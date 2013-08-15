﻿using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("StockBandit Service")]
[assembly: AssemblyDescription("A service to monitor stocks against Bollinger Bands")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("a079814f-2aac-45a2-8cda-9a5f92d130e6")]

// Configure log4net using the .log4net file
[assembly: log4net.Config.XmlConfigurator(ConfigFileExtension = "log4net", Watch = true)]