open System

type Reservation = {
    Date        : DateTime
    Quantity    : int
    IsAccepted  : bool
}

module Globals =
    let capacity         = 100
    let connectionString = "some connection string"

module DB =
    // string -> DateTimeOffset -> Reservation list
    let readReservations (connectionString:string) (date:DateTime) : Reservation list = failwith "readReservations Not Implemented"
    // string -> Reservation -> int
    let createReservation (connectionString:string) (reservation:Reservation) : int = failwith "createReservation Not implemented"

