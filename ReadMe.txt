INTRODUCTION
------------
This is an infrastructure template for creating a C# Windows service. For more details about its capabilities, please see these 3 blog articles: http://sleeksoft.co.uk/public/techblog/2014_10.html

PROJECT GOALS
-------------
Provide Windows service template, so that the developers can focus  on the real work.
Create a service infrastructure that starts/restarts, monitors, and logs the worker thread where the service's work is occurring.
Isolate the service's work from the controlling infrastructure so that any crash is logged properly and doesn't bring down the service. 
Have relatively simple and small code, so that it's easy to understand, maintain, and debug.
Enable the developer to test and debug the service within Visual Studio.
Allow the service to install and uninstall itself from the command-line, without the use of InstallUtil.
Avoid any cross-thread interactions that involve polling or "busy" loops.
Reduce application-level cross-thread interactions that involve shared memory.
Log everything that the service is doing, and especially cross-thread interactions.
Be well documented.
Be well tested.
Be performant.

PROJECT MATURITY
----------------
This is a mature project that has multiple production implementations.

SUPPORTED FEATURES
------------------
Automated configuration and install/de-install of a service from the command line.
Automated logging of information, warnings, and errors.
Isolation of the business logic from the service infrastructure.
Automated drop-and-restart in the event of any unhandled exception in the business logic.
Enables the developer to debug the service within Visual Studio.

SUPPORTED VERSIONS 
------------------
Language: C# 2.0 and upwards. 
Framework: Version 2.0 and upwards. 
Runtime: CLR version 2.0 and upwards. 
IDE: Visual Studio 2010 and upwards.

GETTING STARTED
---------------
Specify the service configuration in its config file.
Add your business logic where indicated, including any logging.
Debug the service by using VS command-line parameter of /d.
Install the service from the command-line as indicated.

ROADMAP
-------
Adding unit tests with MSTest.
Fixing any bugs that are raised.

SUPPORT
-------
You can email the primary author on mark.stephen.pearce@gmail.com. It's likely that I'll fix any significant bugs fairly quickly. 

LICENSE
-------
TL;DR: Very liberal. Basically, you can do whatever you want as long as you include the original copyright.

The MIT License (MIT)

Copyright (c) 2015-2017 Mark Pearce

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NON-INFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
