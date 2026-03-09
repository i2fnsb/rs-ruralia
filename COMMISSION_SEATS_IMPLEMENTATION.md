# Commission Seats and Commissioners Implementation

## Overview
Implemented Commission Seats and Commissioners management panels in the Service Areas page, following the same patterns used for Ordinances.

## Changes Made

### 1. ServiceAreas.razor - Injections Added
```csharp
@inject CommissionSeatsApiClient CommissionSeatsApi
@inject CommissionSeatTypesApiClient CommissionSeatTypesApi
@inject CommissionSeatClassesApiClient CommissionSeatClassesApi
@inject CommissionSeatStatusesApiClient CommissionSeatStatusesApi
@inject CommissionerProfilesApiClient CommissionerProfilesApi
@inject CommissionerStatusesApiClient CommissionerStatusesApi
```

### 2. Commission Seats Panel
**Features:**
- ✅ Display all commission seats for the selected service area
- ✅ Show Seat Type, Class, Status, Term Start/End dates
- ✅ Add new commission seats
- ✅ Edit existing commission seats
- ✅ Delete commission seats
- ✅ Loading states and empty states

**Key Fields:**
- `CommissionSeat.ServiceAreaId` (Required) - Links seat to service area
- `CommissionSeat.CommissionSeatTypeId` - Type of seat
- `CommissionSeat.CommissionSeatClassId` - Class of seat
- `CommissionSeat.CommissionSeatStatusId` - Status (Active, Inactive, etc.)
- `CommissionSeat.TermStartDate` - When the term starts
- `CommissionSeat.TermEndDate` - When the term ends

### 3. Commissioners Panel
**Features:**
- ✅ Display commissioners assigned to seats in the service area
- ✅ Show which seat they're assigned to
- ✅ Show commissioner status and dates
- ✅ Open commissioner details page
- ✅ Only visible when commission seats exist

**Relationship:**
```
Service Area (1) -> (N) Commission Seats (1) -> (N) Commissioners
```

**Key Fields:**
- `CommissionerProfile.CommissionSeatId` (Required) - Links to a seat
- `CommissionerProfile.PersonProfileId` (Required) - Links to person
- `CommissionerProfile.CommissionerStatusId` - Status of commissioner
- `CommissionerProfile.AppointedDate` - When appointed
- `CommissionerProfile.EndDate` - When term ends

### 4. Code-Behind Fields
```csharp
// Commission Seats management fields
private IEnumerable<CommissionSeat>? commissionSeats;
private IEnumerable<CommissionSeatType>? commissionSeatTypes;
private IEnumerable<CommissionSeatClass>? commissionSeatClasses;
private IEnumerable<CommissionSeatStatus>? commissionSeatStatuses;
private bool isLoadingCommissionSeats = false;

// Commissioners management fields
private IEnumerable<CommissionerProfile>? commissioners;
private IEnumerable<CommissionerStatus>? commissionerStatuses;
private bool isLoadingCommissioners = false;
```

### 5. Loading Methods

#### LoadCommissionSeats()
- Loads all commission seats for the selected service area
- Automatically loads commissioners for those seats
- Called when service area is selected

#### LoadCommissionSeatLookups()
- Loads seat types, classes, and statuses (lookup tables)
- Called once on initialization

#### LoadCommissioners()
- Loads all commissioners for seats in the current service area
- Filters by seat IDs that belong to the service area

#### LoadCommissionerStatusLookups()
- Loads commissioner statuses (lookup table)
- Called once on initialization

### 6. Dialog Methods

#### OpenAddCommissionSeatDialog()
- Opens dialog to create a new commission seat
- Pre-populates `ServiceAreaId` with current service area
- Shows dropdowns for Type, Class, Status
- Shows date pickers for term dates

#### OpenEditCommissionSeatDialog(seat)
- Opens dialog to edit existing commission seat
- Same fields as add dialog

#### DeleteCommissionSeat(seat)
- Shows confirmation dialog
- Deletes the commission seat
- Refreshes the list

### 7. CommissionSeatDialog Component
**File:** `rs-ruralia.Web/Components/Shared/CommissionSeatDialog.razor`

**Features:**
- MudDialog with edit form
- Dropdowns for Seat Type, Class, Status
- Date pickers for Term Start/End and Creation/Termination dates
- Validation with DataAnnotationsValidator
- Dynamic button text (Create/Update)

**Parameters:**
- `CommissionSeat` - The seat being edited/created
- `CommissionSeatTypes` - Lookup data
- `CommissionSeatClasses` - Lookup data
- `CommissionSeatStatuses` - Lookup data
- `IsNew` - Whether creating or updating

## UI Layout

### Commission Seats Table
| Type | Class | Status | Term Start | Term End | Actions |
|------|-------|--------|------------|----------|---------|
| Board | At-Large | Active | Jul 1, 2023 | Jun 30, 2026 | Edit / Delete |

### Commissioners Table
| Seat | Person | Status | Appointed Date | End Date | Actions |
|------|--------|--------|----------------|----------|---------|
| Board - At-Large | Person ID: 123 | Active | Jan 15, 2023 | Jun 30, 2026 | View Details |

## Data Flow

```
1. User selects Service Area
   ↓
2. LoadAllVersions() called
   ↓
3. LoadCommissionSeats() called
   ↓
4. LoadCommissioners() called
   ↓
5. UI displays both panels with data
```

## API Endpoints Used

### Commission Seats
- `GET /commission-seats/service-area/{id}` - Get seats by service area
- `POST /commission-seats` - Create new seat
- `PUT /commission-seats/{id}` - Update seat
- `DELETE /commission-seats/{id}` - Delete seat

### Commissioners
- `GET /commissioner-profiles` - Get all commissioners (filtered client-side)

### Lookups
- `GET /commission-seat-types`
- `GET /commission-seat-classes`
- `GET /commission-seat-statuses`
- `GET /commissioner-statuses`

## Notes

1. **Commission Seats** must belong to a service area (`ServiceAreaId` is required)
2. **Commissioners** are linked to commission seats, not directly to service areas
3. The Commissioners panel only shows if commission seats exist
4. Person profiles are referenced by ID (actual names would come from PersonProfile table)
5. All entities use temporal tables (ValidFrom/ValidTo) for history tracking

## Future Enhancements

- [ ] Add person name display (requires PersonProfile lookup)
- [ ] Add "Assign Commissioner" button in Commissioners panel
- [ ] Add inline commissioner editing
- [ ] Add filtering/sorting for seats and commissioners
- [ ] Add validation for term dates (must be July 1 - June 30)
- [ ] Show commissioner history in timeline
