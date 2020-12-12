## Forget ~~FsDepend~~, there is a better simpler dependency injection

The simplest and best dependency injection method I know (and I have tried them all: partial application, 
Interfaces, Flexible types, Reader monad, I even developed my own version: **FsDepend**)
is defined in just one line:

    type Dependency<'T>(def:'T) = member val D = def with get, set

Almost every thing I list as features below for FsDepend is true of this implementation.
It is so much simpler, clean and effective, than all the other options. 
No parameter passing, no monads, no special syntax, simple.

Using the same Reservations example, here is how it is used. 

First we define the injectable elements with `Dependency`:

    module CommonDefs =

        type Reservation = {
            Date        : System.DateTime
            Quantity    : int
            IsAccepted  : bool
        }

        module Globals =
            let capacity          = Dependency 100
            let connectionString  = Dependency "some connection string"

        module DB =
            let readReservations  = Dependency (fun (connectionString:string) (date:System.DateTime   ) -> failwith "readReservations Not Implemented"  : Reservation list)
            let createReservation = Dependency (fun (connectionString:string) (reservation:Reservation) -> failwith "createReservation Not implemented" : int)

and this is how it is used (notice `.D` for instance `DB.readReservations.D`):

    module Reservation =
        open CommonDefs

        let tryAccept reservation =
            let reservedSeats =
                DB.readReservations.D   Globals.connectionString.D reservation.Date |> List.sumBy (fun x -> x.Quantity)
            if reservedSeats + reservation.Quantity <= Globals.capacity.D
            then DB.createReservation.D Globals.connectionString.D { reservation with IsAccepted = true } |> Some
            else None

this is how to inject:

        DB.readReservations     .D <- (fun cs _ -> printfn "readReservations  Connection String: %s" cs;[]        ) 
        DB.createReservation    .D <- (fun cs r -> printfn "createReservation Connection String: %s" cs;r.Quantity) 
        Globals.capacity        .D <-  25
        Globals.connectionString.D <- "new Connx Str"

and run it as normal:

        {
            Date        = System.DateTime.Today
            Quantity    = 5
            IsAccepted  = false
        } 
        |> tryAccept
        |> printfn "%A"

# ~~FsDepend~~ (Deprecated)
Functional Dependency Injection for FSharp

This module provides a novel approach to dependency injection in F#.

Among its features:
- Simple and explicit injection definitions with defaults
- Fast: functions are fully resolved and ready to be used
- Selective, multilevel, partial and ~~localized~~ code injection
- ~~Capacity to print out injectable entries~~
- A natural way to coding progresion with reduced refactoring

please visit the Wiki for the documentation: https://github.com/amieres/FsDepend/wiki
