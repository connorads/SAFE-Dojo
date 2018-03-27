module Api

open DataAccess
open FSharp.Data.UnitSystems.SI.UnitNames
open Giraffe
open Microsoft.AspNetCore.Http
open Saturn
open Shared

let private london = { Latitude = 51.5074; Longitude = 0.1278 }
let invalidPostcode next (ctx:HttpContext) =
    ctx.SetStatusCode 400
    text "Invalid postcode" next ctx

let getDistanceFromLondon postcode next (ctx:HttpContext) = task {
    if Validation.validatePostcode postcode then
        let! location = getLocation postcode
        let distanceToLondon = getDistanceBetweenPositions location.LatLong london
        return! json { Postcode = postcode; Location = location; DistanceToLondon = (distanceToLondon / 1000.<meter>) } next ctx
    else return! invalidPostcode next ctx }

let getCrimeReport postcode next ctx = task {
    if Validation.validatePostcode postcode then
        let! location = getLocation postcode
        let! reports = Crime.getCrimesNearPosition location.LatLong
        let crimes =
            reports
            |> Array.countBy(fun r -> r.category)
            |> Array.sortByDescending snd
            |> Array.map(fun (k, c) -> { Crime = k; Incidents = c })
        return! json crimes next ctx
    else return! invalidPostcode next ctx }

let private asWeatherResponse weather =
    { WeatherResponse.Description =
        weather.consolidated_weather
        |> Array.maxBy(fun w -> w.weather_state_name)
        |> fun w -> w.weather_state_name
      AverageTemperature = weather.consolidated_weather |> Array.averageBy(fun r -> r.the_temp) }


// TODO 1.1 WEATHER: Implement get weather for postcode
let getWeatherForPosition postcode next ctx = task {  
    return! json { Description = ""; AverageTemperature = 0. } next ctx }

let apiRouter = scope {
    pipe_through (pipeline { set_header "x-pipeline-type" "Api" })
    getf "/distance/%s" getDistanceFromLondon
    // TODO: 4.1 CRIME: Add crime endpoint here using the getCrimeReport web part.
    // TODO 1.2 WEATHER: Add weather endpoint here
    }
