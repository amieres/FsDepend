# FsDepend
Functional Dependency Injection for FSharp

This module provides a novel approach to dependency injection in F#.

Among its features:
- Simple explicit code injection
- Automatic multilevel code injection
- Partial localized code injection
- Selective injection
- Capacity to print out injected code
- A natural way to coding with reduced refactoring

## Usage Example
The best way to explain how it works is with examples. 
(Code examples taken from this excellent blog:
https://blog.ploeh.dk/2017/01/30/partial-application-is-dependency-injection/

Since this is going to be an evolving example. We start with  some common code required going forward:

    module CommonDefs =
        open System

        type Reservation = {
            Date        : DateTimeOffset
            Quantity    : int
            IsAccepted  : bool
        }

        module Globals =
            let capacity         = 100
            let connectionString = "some connection string"

        module DB =
            // string -> DateTimeOffset -> Reservation list
            let readReservations (connectionString:string) (date:DateTimeOffset) : Reservation list = failwith "Not Implemented"
            // string -> Reservation -> int
            let createReservation (connectionString:string) (reservation:Reservation) : int = failwith "Not implemented"

### First code version: Everything hardcoded.

The example deals with the creation of a reservation system and evolves around one function called `tryAccept` :

    module PartialApplicationIsDependencyInjection =
        open System
        open CommonDefs

        // Reservation -> int option
        let tryAccept0 reservation : int option =
            let reservedSeats =
                DB.readReservations Globals.connectionString reservation.Date |> List.sumBy (fun x -> x.Quantity)
            if reservedSeats + reservation.Quantity <= Globals.capacity
            then DB.createReservation Globals.connectionString { reservation with IsAccepted = true } |> Some
            else None

The first version `tryAccept0` does not use any dependency injection, it simply uses the code that is above it. In that sense the functionality is fully 'Hard Coded'. Testing can only be done by providing actual connections.

This is how programming normally starts. 
You try to make the code work for the specific case you are trying to solve.
Later on you make the code more generic and testable by, among other things...

## Second version: adding Dependency Injection

So you add Dependency Injection:

    let connectionStringD   = Depend.depend0 <@ Globals.connectionString  @>
    let capacityD           = Depend.depend0 <@ Globals.capacity          @>
    let readReservationsD   = Depend.depend2 <@ DB.readReservations       @>
    let createReservationD  = Depend.depend2 <@ DB.createReservation      @>

    // Depend<(Reservation -> int option)>
    let tryAcceptD = Depend.depend {
        let! connectionString   = connectionStringD
        let! capacity           = capacityD
        let! readReservations   = readReservationsD
        let! createReservation  = createReservationD
        return
            fun reservation ->
                let reservedSeats =
                    readReservations connectionString reservation.Date |> List.sumBy (fun x -> x.Quantity)
                if reservedSeats + reservation.Quantity <= capacity
                then createReservation connectionString { reservation with IsAccepted = true } |> Some
                else None
    }

    // Reservation -> int option
    let tryAccept = tryAcceptD |> Depend.resolver []

This example demonstrates how to declare and reference the dependencies.

The line

    let readReservationsD   = Depend.depend2 <@ DB.readReservations  @>` 
    
provides a definition for a replaceable 
injection that uses by default the current definition of `DB.readReservations`. The last digi of `Depend.depend0` indicates 
the number of parameters the function takes, `depend0` means it is not a function, `depend1` a function with one parameter, 
`depend2` takes 2, and so on, up to 5.
It can only be replaced by a function of the same type: `string -> DateTimeOffset -> Reservation list`. 
It is similar to a reader monad with some key differences.

The capital `D` as a suffix to indicate that it is a `Depend` monad: `readReservationsD` is a dependency injection
for `readReservations`. So `readReservationsD` is of type: `Depend<(string -> DateTimeOffset -> Reservation list)>`.

To facilitate the use of dependencies there is a Computation Expression: **`...= Depend.depend {`**. The first section of `tryAcceptD` retrieves the actual dependent values using `let!` syntax:

    let! readReservations   = readReservationsD

After retrieving all the dependencies we immediately return the function as a lambda with **`return fun reservation ->`**. In this first version the returned lambda function is exactly like the original `tryAccept0` except that it uses the retrieved values instead of the global ones (in modules Global and DB). Notice that the parameters (in this case `reservation`) are in the lambda function not `tryAcceptD`.

Finally the line:

    let tryAccept = tryAcceptD |> Depend.resolver []
    
returns a version of tryAccept that uses the default values for all dependencies. 

We can selectively replace dependencies this way:

    let readReservationsMock connStr date = [ { ... } ]

    let tryAccept = // Reservation -> int option
        tryAcceptD 
        |> Depend.resolver 
            [   Depend.replace0 <@ Globals.capacity    @> 50 
                Depend.replace2 <@ DB.readReservations @> readReservationsMock ]

## Multilevel Injection

The function `tryAccept` does not need to be aware of `connectionString` as that is a concern of `readReservations` and 
`createReservation`. Instead we can bake `connectionString` into the `DB.` functions in an intermediate step like this:

    let readCreateReservationsD = Depend.depend {
        let! connectionString  = connectionStringD
        let! readReservations  = readReservationsD
        let! createReservation = createReservationD
        return (readReservations  connectionString)
              ,(createReservation connectionString)
    }

And refactor `tryAcceptD` this way:

    let tryAcceptD = Depend.depend {
        let! capacity           = capacityD
        let! readReservations2
           , createReservation2 = readCreateReservationsD
        return
            fun reservation ->
                let! reservations   = readReservations2 reservation.Date
                let  reservedSeats  = reservations |> List.sumBy (fun x -> x.Quantity)
                if reservedSeats + reservation.Quantity <= capacity
                then let!        i  = createReservation2 { reservation with IsAccepted = true } 
                     return Some i
                else return None
    }

## Printing the dependency list

    tryAcceptD |> Depend.toString |> printfn "%s"

produces the following output:

    FSI_0002+CommonDefs+DB.createReservation           <fun:getName2@382>
    FSI_0002+CommonDefs+DB.readReservations            <fun:getName2@382>
    FSI_0002+CommonDefs+Globals.capacity               100
    FSI_0002+CommonDefs+Globals.connectionString       "some connection string"
    NoMore <fun:tryAcceptD@1241-4>
    
## Generic functions

Generic functions cannot be used directly, for instance, the following code causes an error:

    let log  fmt = Printf.ksprintf (printfn "%s") fmt
    let logD ()  = Depend.depend0 <@ log @>   
    
    let tryAcceptD = Depend.depend {
        let! log = logD() // use a function to avoid Generic Value error
        ...
        return
            fun reservation ->
                log "Capacity: %d" capacity                     // First use fixes a type for log
                log "Connection String %s" connectionString     // ERROR two different types for log
                ...

Function `log` outside of `tryAcceptD` is generic, but the instantiation in `tryAcceptD` cannot be generic.
Instead we can use an interface like this:

    let   log fmt = Printf.ksprintf (printfn "%s") fmt
    type ILogger  = abstract Log : Printf.StringFormat<'a,unit> -> 'a
    let   logger  = { new ILogger with member __.Log fmt = log fmt }
    let   loggerD = Depend.depend0 <@ logger @>

    let tryAcceptD = Depend.depend {
        let! logger = loggerD
        ...
        return
            fun reservation ->
                logger.Log "Capacity: %d" capacity
                logger.Log "Connection String %s" connectionString  // no problem
                ...

