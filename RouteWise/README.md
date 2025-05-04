# RouteWise
 
## Overview

RouteWise is a highly customizable flight search API that integrates with [Amadeus](https://developers.amadeus.com/) to provide flight offers, multi-city searches, and destination exploration.

## Features

- Highly Customizable Flight Searches

  - Supports multiple search criteria, including origin, departure date, layover, and price filtering.

  - Offers one-way, round-trip, and multi-city flight search options.

- Layover and Stopover Configurations

  - Allows customization of layovers, including duration, location, and amount.

  - Supports filtering for non-stop flights.

- Dynamic Date Selection

  - Supports searching by year, month, days of the week, or fixed duration.
 
  - Supports searching within day ranges e.g. _Flights with departure on a Friday and return on the Sunday_

- Error Handling Middleware

  - Provides structured error responses for better debugging and client-side handling.

- RESTful API with JSON Responses

  - Designed for seamless integration with front-end applications and third-party services.

- Swagger Support

  - Auto-generated API endpoint testing

## Technologies Used

- .NET 8

- ASP.NET Core

- Amadeus API

- Swagger for API documentation

- Dependency Injection with HttpClient

- Middleware for exception handling
