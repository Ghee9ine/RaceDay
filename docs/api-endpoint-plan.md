# RaceDay API Endpoint Plan

This plan covers every endpoint the RaceDay API will expose, grouped by resource. It is written
before any API code exists (Part 1) and Part 2's implementation is expected to match it closely.

Roles: **None** = public, no token required. **Any** = any authenticated user, either role.
**Organiser** / **Participant** = must hold that role. "(owner)" means the endpoint additionally
checks that the authenticated organiser owns the event/category being modified, not just that
their role is Organiser.

**Authenticated** means the caller holds a valid `.RaceDay.Session` cookie issued by `POST
/api/auth/login`. There is no bearer token: the server keeps a session store (keyed by that
cookie) and writes the caller's `UserId` and `Role` into it at login. Every later request on the
same session is authenticated from that stored state, not from anything the client sends beyond
the cookie itself.

## Authentication

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/auth/register | Creates a new user account as either an Organiser or a Participant. Does not log the new user in. | None | { fullName, email, password, role, phoneNumber } | 201 Created - user id and role, with a Location header pointing to /api/users/me; 400 Bad Request - validation failed (including an invalid role); 409 Conflict - email already registered |
| POST | /api/auth/login | Authenticates a user and starts a server-side session. The session store records the user's id and role; the response sets the `.RaceDay.Session` cookie, which the client must send on every later request. | None | { email, password } | 200 OK - Set-Cookie: .RaceDay.Session, body { userId, fullName, email, role }; 401 Unauthorized - invalid credentials (generic message; does not reveal whether the email exists) |
| POST | /api/auth/logout | Ends the authenticated user's session and clears the session cookie. | Any | None | 204 No Content; 401 Unauthorized - no active session |

## User Profile

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/users/me | Returns the profile of the currently authenticated user, including their profile picture URL once set. | Any | None | 200 OK - { userId, fullName, email, role, phoneNumber, profilePictureUrl, createdAt }; 401 Unauthorized |
| PUT | /api/users/me | Updates the authenticated user's own profile details. | Any | { fullName, phoneNumber } | 200 OK - updated profile; 400 Bad Request |
| POST | /api/users/me/profile-picture | **Implemented in Part 3.** Uploads the authenticated user's profile picture to Azure Blob Storage and stores its URL. | Any | multipart/form-data file | 200 OK - { profilePictureUrl }; 400 Bad Request; 401 Unauthorized |

## Events

Distance is not a field on the event itself - one event can offer several distances (e.g. an
87km ultramarathon and a 21km half marathon on the same day), so `DistanceKm` lives on
`Categories`. Every event response therefore includes a computed `distancesKm` array drawn from
its categories. "Location" in the brief maps to `Province` + `Venue`; "event type" maps to
`DisciplineType`.

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events | Lists all **published** events, with optional `province`, `disciplineType`, `from` and `to` query filters, ordered by date, so participants can browse events. | None | None | 200 OK - array of { eventId, name, description, disciplineType, eventDate, startTime, province, venue, status, bannerImageUrl, organiserName, distancesKm[] } |
| GET | /api/events/{id} | Returns full details for a single event, including a `categories[]` summary. A Draft event returns 404 to anyone who isn't its owner. | None | None | 200 OK - event details; 404 Not Found |
| GET | /api/events/mine | Lists every event owned by the authenticated organiser, in any status (including Draft), each with a `totalEnrolments` count (Confirmed enrolments only) for an organiser dashboard. | Organiser | None | 200 OK - array of events with totalEnrolments; 401 Unauthorized |
| POST | /api/events | Creates a new event owned by the authenticated organiser. The organiser id is taken from the session, never the request body. Defaults to Published if `status` is omitted. | Organiser | { name, description, disciplineType, eventDate, startTime, province, venue, status? } | 201 Created - new event; 400 Bad Request |
| PUT | /api/events/{id} | Updates an existing event's details. | Organiser (owner) | { name, description, disciplineType, eventDate, startTime, province, venue, status } | 200 OK - updated event; 403 Forbidden - not the owner; 404 Not Found |
| DELETE | /api/events/{id} | Deletes an event and its categories (and their routes), provided no enrolments exist in any of its categories. | Organiser (owner) | None | 204 No Content; 403 Forbidden; 404 Not Found; 409 Conflict - event already has enrolments |
| POST | /api/events/{id}/banner | **Implemented in Part 3.** Uploads an event's banner image to Azure Blob Storage and stores its URL. | Organiser (owner) | multipart/form-data file | 200 OK - { bannerImageUrl }; 403 Forbidden; 404 Not Found |

## Categories

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events/{eventId}/categories | Lists all categories offered under a specific event. | None | None | 200 OK - array of categories; 404 Not Found - event does not exist |
| GET | /api/categories/{id} | Returns details for a single category. | None | None | 200 OK - category details; 404 Not Found |
| POST | /api/events/{eventId}/categories | Adds a new category (e.g. 10km, 21.1km, an age group such as Under 20 or Veteran) to an event. | Organiser (owner) | { name, distanceKm, entryFee, maxParticipants, ageGroup? } | 201 Created - new category; 400 Bad Request - duplicate name for this event; 403 Forbidden; 404 Not Found |
| PUT | /api/categories/{id} | Updates a category's details. | Organiser (owner) | { name, distanceKm, entryFee, maxParticipants, ageGroup? } | 200 OK - updated category; 403 Forbidden; 404 Not Found |
| DELETE | /api/categories/{id} | Removes a category and its route, provided no enrolments exist for it. | Organiser (owner) | None | 204 No Content; 403 Forbidden; 404 Not Found; 409 Conflict - category already has enrolments |

## Event Enrolments

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/categories/{categoryId}/enrolments | Enters the authenticated participant into a category and assigns a bib number (format `C{categoryId}-{enrolmentId}`, zero-padded). If the participant previously cancelled an enrolment in this category, it is reactivated instead of inserted again. | Participant | None (participant identity taken from the session) | 201 Created - { enrolmentId, eventId, eventName, categoryId, categoryName, bibNumber, enrolmentDate, status }; 404 Not Found - category does not exist; 409 Conflict - event not published or already past, already Confirmed, or category full |
| GET | /api/enrolments/me | Returns all of the authenticated participant's own enrolments, past and upcoming, newest first. | Participant | None | 200 OK - array of enrolments |
| GET | /api/events/{eventId}/enrolments | Lists every enrolment across all categories of an event, with participant name and email, for the organiser to manage race-day logistics. | Organiser (owner) | None | 200 OK - array of enrolments; 403 Forbidden; 404 Not Found |
| DELETE | /api/enrolments/{id} | Cancels (soft delete - sets Status to Cancelled) the authenticated participant's own enrolment. | Participant (owner) | None | 204 No Content; 403 Forbidden - not the owner; 404 Not Found; 409 Conflict - a result has already been captured |

## Results

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/enrolments/{enrolmentId}/results | Captures a finish result for a participant's enrolment. The capturing organiser's id is taken from the session. `Finished` requires `finishTime` and `position`; `DNF`/`DSQ` allow both to be null. | Organiser (owner of the event) | { finishTime, position, status } | 201 Created - result record; 403 Forbidden; 404 Not Found; 409 Conflict - result already captured, or the enrolment is cancelled |
| PUT | /api/results/{id} | Corrects a previously captured result. | Organiser (owner of the event) | { finishTime, position, status } | 200 OK - updated result; 403 Forbidden; 404 Not Found |
| GET | /api/results/me | Returns the authenticated participant's full personal result history, each with `totalFinishers` for that category (e.g. "47th out of 312 finishers"). | Participant | None | 200 OK - array of { eventName, eventDate, categoryName, distanceKm, finishTime, position, totalFinishers, status } |
| GET | /api/categories/{categoryId}/results | Returns the public leaderboard for a category: Finished results ordered by finish time, then DNF/DSQ. | None | None | 200 OK - ranked array of results; 404 Not Found |

## Race-Day Preparation (Routes & Weather)

These endpoints support the background's requirement that participants can "prepare for race day
using live weather and route information."

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/categories/{categoryId}/route | Returns the route details (start/end point, elevation, map) for a category. | None | None | 200 OK - route details; 404 Not Found - no route captured yet |
| PUT | /api/categories/{categoryId}/route | Creates or replaces the route for a category. | Organiser (owner) | { startPoint, endPoint, elevationGainM, mapUrl } | 200 OK - upserted route; 403 Forbidden; 404 Not Found |
| GET | /api/events/{eventId}/weather | Returns the most recent weather snapshot recorded for an event's date/location. | Any | None | 200 OK - latest weather snapshot; 404 Not Found - no forecast recorded yet |
