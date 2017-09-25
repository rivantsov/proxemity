# Proxemity - .NET proxy emitter

**Proxemity** is a libraray for emitting proxy classes for interfaces. The emitted proxy class inherits from some base class and implements one or more interfaces. The emitted methods/properties redirect all calls to the designated proxy target - an object referenced by a field or property in the base class. The redirect call in the implementation method is guided by the 'spec' returned for each method by the emit controller that you provide. 

This project is a spin-off of the VITA project - it is an extension and generalization of the dynamic emit functionality that VITA uses for emitting entity classes at runtime. These classes are for objects that are behind the entity interfaces at runtime. The next version of VITA will be using Proxemity for IL emit. Initially I tried some existing proxy emitters, but found that they do not provide the functionality needed for VITA. 

There are some other interesting cases where Proxemity might be used. One of the planned uses is generating strong API client based on an interface describing the API, with URL endpoints provided in method attributes. The generated proxy would redirect all calls to the underlying generic Api client (based on HttpClient like VITA's WebApiClient or your own http client). This is in the plans - to spin-off Web Api Client from VITA and add strongly-typed client generation. 


 



