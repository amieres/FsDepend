# FsDepend
Functional Dependency Injection for FSharp

This module provides a novel approach to dependency injection in F#.

Among its features:
- Simple explicit code injection
- Progresive coding with reduced refactoring.
- Automatic multilevel code injection
- Partial localized code injection
- Capacity to print out injected code

## Usage Example
The best way to explain how it works is with examples. 
(Code examples taken from this excellent blog:
https://blog.ploeh.dk/2017/01/30/partial-application-is-dependency-injection/

Since this is going to be an evolutionary example. We start with  some common code required going forward:

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

The first version `tryAccept0` does not use any dependency injection, it simply uses the code that is above. In that sense the functionality is fully 'Hard Coded'. Testing can only be done by providing actual connections.

This is how programming most code normally starts. 
You try to make the code work for the specific case you are trying to solve.
Later on you can make code more generic and testable.

## Dependency Injection

So you add Dependency Injection:

    let connectionStringD   = Depend.depend0 <@ Globals.connectionString  @>
    let capacityD           = Depend.depend0 <@ Globals.capacity          @>
    let readReservationsD   = Depend.depend2 <@ DB     .readReservations  @>
    let createReservationD  = Depend.depend2 <@ DB     .createReservation @>

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

    let tryAccept = tryAcceptD |> Depend.resolver []

This example demonstrates how to declare and reference the dependencies.

The line `let readReservationsD   = Depend.depend2 <@ DB.readReservations  @>` provides a definition for a replaceable 
injection that uses by default the current definition of `DB.readReservations`. It can only be replaced by a function of the same type: `string -> DateTimeOffset -> Reservation list`. It is similar to a reader monad with some key differences.

I use capital D as a suffix to indicate the monadic type of the element: `readReservationsD` is a dependency injection
for `readReservations`. So `readReservationsD` is of type: `Depend<(string -> DateTimeOffset -> Reservation list)>`.

To use the dependencies there is a Computation Expression: `Depend.depend`. The first section of `tryAcceptD` retrieves the actual dependent values using `let!` syntax. 

After retrieving all the dependencies we immediately return the function: with `return fun reservation ->`. In this first version the returned function is exactly like the original `tryAccept0` except that it uses the retrieved values instead of the global ones (in modules Global and DB). Notice that the parameters (in this case `reservation`) are in the inner function not the outside one.


