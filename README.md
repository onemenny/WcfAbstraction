Wcf Abstraction
==============

## This is working application for a client-server architecture using WCF abstraction. It can be used to start up your own client server app and to give you a great kick start for common uses like behaviors, serialization, DI and IOC, caching and best practice. ##
 
The project is built with 3 sections: 


1. The client, currently implemented as a WPF application, but can be any other application
2.	The server. Implemented as a Windows service, but can also be implemented as self hosted or IIS hosted application. The server also includes:    
a.the contracts to communicate with the clients (e.g. entities, service interfaces) reside both in client and server side.    
b.	the server itself – service interfaces implementation    
c.	the windows service – the host of the application
3.	common – common helpers for use (reside both in client and server side)

## Starting the Project ##
When running the project, you need to setup multiple start up project for the WcfAbstraction.Client .Windows & WcfAbstraction.Server.Windows. Before starting the solution make sure you build it. Once the solution is running please confirm the Windows Firewall setting for private network access only.

You will need to wait till the server is loaded (the server console app will state so) and only then try the client communication.

## Third Party Helper##
I have used www.idesign.net/ (Juval Lowy) code for the generic implementation of ServiceHost (ServiceHost<T>). 
I have used Unity from the Enterprise Library for the DI and IOC container
I have used Moq for the unit testing (used in conjunction with Unity to setup different app.config for unit testing instead of opening real services)


## app.config##
Please notice the various (WCF) settings in the app.config files of both client and server.  The enterprise Library Unity is used differently in the client and server. On the client side, it is used to insatiate the services for the client:
`UnityRegistry.GetService<ITestService>().AssertArgumentNotNull_GenericFault(null);` 
And on the server they are used to get a service reference within other service, and not for the WCF host instantiation, which is done by the regular configuration section in the config file.

## The Heart of it all##
The heart of the system is the proxy implementation. This is the abstraction of the WCF layer. Since WCF implementation itself implements the proxy client and server from the System.Runtime.Remoting  DLL. This gives you as a developer full control on the communication process. Since you get to intercept it before the invocation of the remoting method, you can view the arguments and validate them, you can handlecommunication exception and much much more. 

** So give it a try before you go to the regular MS implementation with all the wizards and the auto generated code that you don’t have a clue about **







