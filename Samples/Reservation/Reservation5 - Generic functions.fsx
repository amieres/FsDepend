#load "CommonDefs.fsx"
#load @"..\..\FsDepend.fs"

open System
open CommonDefs

let connectionStringD   = Depend.depend0 <@ Globals.connectionString  @>
let capacityD           = Depend.depend0 <@ Globals.capacity          @>
let readReservationsD   = Depend.depend2 <@ DB.readReservations       @>
let createReservationD  = Depend.depend2 <@ DB.createReservation      @>

let   log fmt = Printf.ksprintf (printfn "%s") fmt
type ILogger  = abstract Log : Printf.StringFormat<'a,unit> -> 'a
let   logger  = { new ILogger with member __.Log fmt = log fmt }
let   loggerD = Depend.depend0 <@ logger @>

// Depend<(Reservation -> int option)>
let tryAcceptD = Depend.depend {
    let! logger             = loggerD
    let! connectionString   = connectionStringD
    let! capacity           = capacityD
    let! readReservations   = readReservationsD
    let! createReservation  = createReservationD

    return
        fun reservation ->
            logger.Log "Capacity: %d" capacity
            logger.Log "Connection String: %s" connectionString
            let reservedSeats =
                readReservations connectionString reservation.Date |> List.sumBy (fun x -> x.Quantity)
            if reservedSeats + reservation.Quantity <= capacity
            then createReservation connectionString { reservation with IsAccepted = true } |> Some
            else None
}

// Reservation -> int option
let tryAccept = 
    tryAcceptD
    |> Depend.resolver 
        [   Depend.replace0 <@ Globals.capacity    @> 50 
            Depend.replace2 <@ DB.readReservations  @> (fun _ _ -> []        ) 
            Depend.replace2 <@ DB.createReservation @> (fun _ r -> r.Quantity) 
        ]

{
    Date        = DateTime.Today
    Quantity    = 5
    IsAccepted  = false
} 
|> tryAccept
|> printfn "%A"
