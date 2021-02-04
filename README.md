# Causality

We have preset values for OfflineMode to disabled, Use indexedDB to enabled and then we ran the WarmUp for the grpc server in the cascading component Initializer.

The project is a wasm hosted by .net core and support .net5, grpc, linq via grpc, sqlserver, server side cache into memory, client side cache into localstorage and indexedDB.

The data model used here is causality with the structure (event, class, cause, effect, exclude, meta and user)
There are two seperated interfaces for clients, grpc and rest, where both use all of the benefits above
