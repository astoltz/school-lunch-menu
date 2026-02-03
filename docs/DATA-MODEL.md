# LINQ Connect API Data Model

This document describes the LINQ Connect REST API endpoints used by the application, their parameters, and response structures.

## Base URL

```
https://api.linqconnect.com/api
```

All endpoints are unauthenticated GET requests returning JSON.

## Endpoints

### 1. FamilyMenuIdentifier

Returns district information, building list, and the menu identifier code.

**URL**: `GET /FamilyMenuIdentifier?identifier={identifier}`

**Parameters**:
| Parameter | Example | Description |
|---|---|---|
| `identifier` | `YVAM38` | Short alphanumeric code for the school's family menu |

**Response** (`FamilyMenuIdentifierResponse`):
```json
{
  "DistrictId": "47ce70b9-238e-ea11-bd68-f554d510c22b",
  "DistrictName": "Lakeville Area Schools",
  "Buildings": [
    {
      "BuildingId": "c895c20a-5e8e-ea11-bd68-f554d510c22b",
      "Name": "Century Middle School"
    }
  ],
  "MenuNotification": "...",
  "Identifier": "YVAM38"
}
```

| Field | Type | Description |
|---|---|---|
| `DistrictId` | string (UUID) | Unique identifier for the school district |
| `DistrictName` | string | Display name of the district |
| `Buildings` | array | List of school buildings in the district |
| `Buildings[].BuildingId` | string (UUID) | Unique identifier for the building |
| `Buildings[].Name` | string | Display name of the building |
| `MenuNotification` | string or null | Optional notification message |
| `Identifier` | string | The same identifier code passed in the request |

---

### 2. FamilyAllergy

Returns the list of allergens configured for a district.

**URL**: `GET /FamilyAllergy?districtId={districtId}`

**Parameters**:
| Parameter | Example | Description |
|---|---|---|
| `districtId` | `47ce70b9-238e-ea11-bd68-f554d510c22b` | UUID of the district |

**Response**: JSON array of `AllergyItem`:
```json
[
  {
    "AllergyId": "4dce70b9-238e-ea11-bd68-f554d510c22b",
    "SortOrder": 1,
    "Name": "Milk"
  },
  {
    "AllergyId": "4ece70b9-238e-ea11-bd68-f554d510c22b",
    "SortOrder": 2,
    "Name": "Egg"
  }
]
```

| Field | Type | Description |
|---|---|---|
| `AllergyId` | string (UUID) | Unique identifier for the allergen |
| `SortOrder` | int | Display sort order |
| `Name` | string | Display name (e.g., "Milk", "Egg", "Peanut", "Tree Nut", "Wheat", "Soy", "Fish", "Shellfish", "Sesame") |

There are 17 allergens total in a typical LINQ Connect district.

**Known Allergen UUIDs**:
| Allergen | UUID |
|---|---|
| Milk | `4dce70b9-238e-ea11-bd68-f554d510c22b` |

---

### 3. FamilyMenu

Returns the full menu tree for a building over a date range, including academic calendar data.

**URL**: `GET /FamilyMenu?buildingId={buildingId}&districtId={districtId}&startDate={startDate}&endDate={endDate}`

**Parameters**:
| Parameter | Example | Description |
|---|---|---|
| `buildingId` | `c895c20a-5e8e-ea11-bd68-f554d510c22b` | UUID of the school building |
| `districtId` | `47ce70b9-238e-ea11-bd68-f554d510c22b` | UUID of the district |
| `startDate` | `2-1-2026` | Start date in **M-d-yyyy** format |
| `endDate` | `2-28-2026` | End date in **M-d-yyyy** format |

**Date format note**: Request parameters use `M-d-yyyy` (no leading zeros). Response date strings use `M/d/yyyy` (e.g., `"2/2/2026"`).

**Response** (`FamilyMenuResponse`):

```json
{
  "FamilyMenuSessions": [ ... ],
  "AcademicCalendars": [ ... ]
}
```

#### Menu Hierarchy

```
FamilyMenuResponse
  +-- FamilyMenuSessions[]           (e.g., "Breakfast", "Lunch")
  |     +-- ServingSessionKey         (base64 string)
  |     +-- ServingSessionId          (string)
  |     +-- ServingSession            (display name, e.g., "Lunch")
  |     +-- MenuPlans[]              (e.g., "Lunch - MS", "Big Cat Cafe - MS")
  |           +-- MenuPlanName        (string)
  |           +-- MenuPlanId          (string)
  |           +-- Days[]
  |                 +-- Date          (string, M/d/yyyy)
  |                 +-- AcademicCalenderId  (string or null)
  |                 +-- MenuMeals[]
  |                       +-- MenuPlanName   (string)
  |                       +-- MenuMealName   (e.g., "Monday 1 - MS")
  |                       +-- MenuMealId     (string)
  |                       +-- RecipeCategories[]
  |                             +-- CategoryName   (e.g., "Main Entrees", "Sides")
  |                             +-- Color          (hex string, e.g., "#000000")
  |                             +-- IsEntree       (bool)
  |                             +-- Recipes[]
  |                                   +-- ItemId              (string)
  |                                   +-- RecipeIdentifier    (e.g., "R2410")
  |                                   +-- RecipeName          (display name)
  |                                   +-- ServingSize         (e.g., "Sandwich")
  |                                   +-- GramPerServing      (double)
  |                                   +-- HasNutrients        (bool)
  |                                   +-- Nutrients[]
  |                                   |     +-- Name          (e.g., "Calories")
  |                                   |     +-- Value         (double)
  |                                   |     +-- Unit          (e.g., "kcals", "g", "mg")
  |                                   |     +-- Abbreviation  (e.g., "Cal")
  |                                   |     +-- HasMissingNutrients (bool)
  |                                   +-- Allergens[]         (list of UUID strings)
  |                                   +-- ReligiousRestrictions[]  (list of UUID strings)
  |                                   +-- DietaryRestrictions[]    (list of UUID strings)
  |
  +-- AcademicCalendars[]
        +-- AcademicCalendarId        (string)
        +-- Days[]
              +-- Date                (string, M/d/yyyy)
              +-- Note                (e.g., "No School", "President's Day")
```

#### Important Notes

- **Allergens on recipes are UUID strings**, not objects. They match the `AllergyId` values from the `FamilyAllergy` endpoint.
- **`IsEntree`** on `RecipeCategory` indicates whether the category contains main entree items. The application only checks entree categories for allergen safety.
- **Menu plan names** vary by school (e.g., `"Lunch - MS"`, `"Big Cat Cafe - MS"`). The analyzer discovers all plans dynamically from the selected session -- no hardcoded prefixes.
- **Days with no `MenuMeals`** but with an `AcademicCalenderId` are typically no-school days.

---

### 4. FamilyMenuMeals

Returns meal definitions for a district.

**URL**: `GET /FamilyMenuMeals?districtId={districtId}`

**Parameters**:
| Parameter | Example | Description |
|---|---|---|
| `districtId` | `47ce70b9-238e-ea11-bd68-f554d510c22b` | UUID of the district |

**Response**: JSON array of `MealItem`:
```json
[
  {
    "MealId": "abc123",
    "Name": "Monday 1",
    "SortOrder": 1
  }
]
```

| Field | Type | Description |
|---|---|---|
| `MealId` | string | Unique identifier for the meal |
| `Name` | string | Display name of the meal |
| `SortOrder` | int | Display sort order |

---

## Academic Calendar Structure

Academic calendar data is embedded in the `FamilyMenu` response. Each `AcademicCalendar` contains a list of notable days with notes.

Menu days reference the academic calendar via `AcademicCalenderId`. The application uses this to determine no-school days: if the academic note contains "No School" (case-insensitive), the day is classified as `IsNoSchool`. Other notes (e.g., "President's Day" without "No School") are displayed as special notes on the calendar.

## HAR File Support

The application can parse all three key responses (`FamilyMenu`, `FamilyAllergy`, `FamilyMenuIdentifier`) from a single HAR file. The `HarFileService` iterates through HAR log entries, matches URLs by substring, and deserializes the response body text. This provides an offline fallback when the API is unavailable or for development/testing.
