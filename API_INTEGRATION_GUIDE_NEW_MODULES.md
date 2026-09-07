# Frontend API Integration Guide — 5 New Modules

Backend is built and live in the local dev DB (`Medsafehub1`). This guide is everything
needed to wire the 5 already-built frontend pages (currently `localStorage`/mock-array/
no-op) to these real endpoints — no backend questions should come up.

**Ground rules, same for every module:**
- Auth: send the existing JWT the same way every other `src/API/*.js` file already does
  (via `apiClient`'s interceptor) — nothing new to configure.
- Every list/dashboard endpoint below is `Admin`-only; the app already redirects
  non-Admins away from those routes via `ROUTE_ACCESS` in `src/data/permissions.js`, so
  this should never surface as a 403 in normal use.
- Every submit/create endpoint is open to any logged-in role.
- **Dropdown option arrays, DOI/PubMed mock lookup, and every hardcoded `<Select>` stay
  exactly as they are today** — none of that becomes API-driven. Only the final
  save/submit/list calls change.
- Response field names are camelCase (ASP.NET Core's default JSON casing) even though the
  C# DTOs are PascalCase — e.g. `StudentName` → `studentName`.

---

## 1. Student Feedback

**Replaces:** `src/data/studentFeedbackStore.js`'s `addFeedbackEntry()`/`getFeedbackEntries()`
(delete this file once both pages below are wired), called from
`StudentFeedbackForm.jsx`'s `handleSubmit` and `StudentFeedbackList.jsx`'s data load.
`FeedbackDetailModal.jsx` needs no change — it already just receives a row object as a prop.

### Endpoints

**`POST /api/student-feedback`** — any logged-in user.
Request:
```json
{
  "studentName": "Jane Doe",
  "overallQuality": 5,
  "instructorClarity": 4,
  "instructorRatingsJson": "{\"knowledgeable\":5,\"prepared\":4,\"participation\":5,\"timelyFeedback\":4}",
  "instructorComments": "Great class",
  "materialsSufficient": "yes",
  "materialsComments": "",
  "environmentRating": 5,
  "supportServicesJson": "{\"academicAdvising\":\"yes\",\"technicalSupport\":\"uncertain\"}"
}
```
Response: `200` with the same fields plus `id`, `createdAt`.

**`GET /api/student-feedback`** — Admin only. Returns an array of the same shape, newest first.

### `src/API/studentFeedbackApi.js` (new file)
```js
import { useMutation, useQueryClient } from '@tanstack/react-query';
import apiClient from './apiClient';
import { handleSuccess, handleError } from './apiUtils';
import { useGet } from './apiService';
import { ENDPOINTS } from './endpoints';

const QUERY_KEY = 'studentFeedback';

// GET /api/student-feedback — Admin only.
export const useStudentFeedbackList = (options = {}) =>
  useGet([QUERY_KEY], ENDPOINTS.STUDENT_FEEDBACK.LIST, options);

// mutate({ studentName, overallQuality, instructorClarity, instructorRatingsJson,
//          instructorComments, materialsSufficient, materialsComments,
//          environmentRating, supportServicesJson }) — any logged-in role.
export const useSubmitStudentFeedback = (options = {}) => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload) => {
      const response = await apiClient.post(ENDPOINTS.STUDENT_FEEDBACK.SUBMIT, payload).catch(handleError);
      return handleSuccess(response);
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [QUERY_KEY] }),
    ...options,
  });
};
```

### `endpoints.js` addition
```js
STUDENT_FEEDBACK: {
  LIST:   '/student-feedback',  // GET  Admin
  SUBMIT: '/student-feedback',  // POST all roles
},
```

### Wiring notes
- `StudentFeedbackForm.jsx`: replace the `addFeedbackEntry({...})` call in `handleSubmit`
  with `useSubmitStudentFeedback().mutate({...})`, same field names — `instructorRatings`
  and `supportServices` objects need `JSON.stringify(...)` before sending (backend stores
  them as JSON strings), matching what the form already builds as plain objects.
- `StudentFeedbackList.jsx`: replace whatever reads `getFeedbackEntries()`/subscribes via
  `subscribeFeedbackEntries` with `const { data } = useStudentFeedbackList()`. When
  displaying `instructorRatingsJson`/`supportServicesJson`, `JSON.parse(...)` them back —
  `FeedbackDetailModal.jsx` expects plain objects (`entry.instructorRatings`), so parse
  before passing the row down, or parse inside the modal.

---

## 2. CPD & Education

**Replaces:** `CPDActivityForm.jsx`'s `handleSubmit`/`handleSaveDraft` (currently pure UI
stubs — no store call exists to remove, just wire these directly) and
`CPDActivityDashboard.jsx`'s hardcoded `ACTIVITIES` array.

### Endpoints

**`POST /api/cpd-activities`** — any logged-in user.
Request:
```json
{
  "category": "cat2",
  "subcategory": "Education and Training",
  "activity": "Preparation for mentoring students, trainees or peers",
  "activityType": "Preparation for mentoring students, trainees or peers",
  "location": "Qatar",
  "format": "Face-to-face",
  "title": "PharmD student rotation",
  "activityDate": "2026-08-31",
  "hours": 10,
  "reflection": "Improved my skills in developing structured learning modules...",
  "comments": "",
  "attachmentId": null,
  "saveAsDraft": false
}
```
Response: same fields plus `id`, `referenceCode` (server-generated, e.g. `CPD-2026-4821`),
`credits` (server-computed — Category 2 caps at 10 regardless of `hours`), `status`
(`"draft"` if `saveAsDraft` was true, else `"pending"`), `reviewFeedback` (always `null`
today — no review UI exists yet to set it), `createdAt`.

**`GET /api/cpd-activities`** — Admin only. Array of the same shape, newest first.

### `src/API/cpdActivityApi.js` (new file)
```js
import { useMutation, useQueryClient } from '@tanstack/react-query';
import apiClient from './apiClient';
import { handleSuccess, handleError } from './apiUtils';
import { useGet } from './apiService';
import { ENDPOINTS } from './endpoints';

const QUERY_KEY = 'cpdActivities';

// GET /api/cpd-activities — Admin only.
export const useCpdActivityList = (options = {}) =>
  useGet([QUERY_KEY], ENDPOINTS.CPD_ACTIVITIES.LIST, options);

// mutate({ category, subcategory, activity, activityType, location, format, title,
//          activityDate, hours, reflection, comments?, attachmentId?, saveAsDraft })
export const useSubmitCpdActivity = (options = {}) => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload) => {
      const response = await apiClient.post(ENDPOINTS.CPD_ACTIVITIES.SUBMIT, payload).catch(handleError);
      return handleSuccess(response);
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [QUERY_KEY] }),
    ...options,
  });
};
```

### `endpoints.js` addition
```js
CPD_ACTIVITIES: {
  LIST:   '/cpd-activities',  // GET  Admin
  SUBMIT: '/cpd-activities',  // POST all roles — { ..., saveAsDraft: bool }
},
```

### Wiring notes
- `CPDActivityForm.jsx`: `handleSaveDraft` calls the mutation with `saveAsDraft: true`;
  `handleSubmit` calls it with `saveAsDraft: false`. The frontend's own `eligibleCredits`
  calculation can stay for the on-screen preview, but the server recomputes and stores its
  own — trust the response's `credits`, not the locally-computed one, if you show a
  post-submit confirmation.
- File upload (`file` state): this guide doesn't cover the attachment upload endpoint
  itself — check with the backend on `IIncidentAttachmentService`'s existing upload route
  and pass the returned attachment id as `attachmentId` here.
- `CPDActivityDashboard.jsx`: replace the hardcoded `ACTIVITIES` array with
  `const { data } = useCpdActivityList()`. Field names differ slightly from the mock data
  you're replacing — mock used `subtitle`/`credits: "10 requested"` (string); the real API
  returns `credits` as a number and no `subtitle` — adjust the table's `columns`/render
  functions accordingly (this is real UI rework, not just a data-source swap, since the
  mock data's shape doesn't 100% match the real API response).

---

## 3. Clinical Pharmacy Intervention

**Replaces:** `ClinicalPharmacyInterventionForm.jsx`'s submit handler (no persistence call
found — wire directly) and `InterventionDashboard.jsx`'s data source.

### Endpoints

**`POST /api/clinical-pharmacy-interventions`** — any logged-in user. Request body: every
field from `INITIAL_FORM_DATA` in `interventionData.js`, same names, all optional strings,
plus `estimatedSaving` (number or null) and `attachmentIdsJson` (a JSON-stringified array
of attachment ids) and `draftStatus` (`"Draft"` or `"Submitted"`). Example (trimmed):
```json
{
  "mrn": "12345", "patientName": "John Smith", "sex": "Male",
  "medication": "Warfarin", "route": "Oral", "frequency": "Once daily",
  "interventionType": "Dose Adjustment", "identifiedProblem": "Supratherapeutic INR",
  "recommendedAction": "Reduce dose to 2.5mg",
  "importance": "Significant", "estimatedSaving": 150.00, "currency": "SAR",
  "prescriberName": "Dr. Ahmed", "contactMethod": "Phone",
  "outcome": "Accepted", "followupRequired": "Yes",
  "additionalNotes": "",
  "draftStatus": "Submitted",
  "attachmentIdsJson": "[]"
}
```
Response: same fields plus `id`, `createdAt`.

**`GET /api/clinical-pharmacy-interventions`** — Admin only. Array of the same shape.

### `src/API/clinicalPharmacyInterventionApi.js` (new file)
```js
import { useMutation, useQueryClient } from '@tanstack/react-query';
import apiClient from './apiClient';
import { handleSuccess, handleError } from './apiUtils';
import { useGet } from './apiService';
import { ENDPOINTS } from './endpoints';

const QUERY_KEY = 'clinicalPharmacyInterventions';

// GET /api/clinical-pharmacy-interventions — Admin only.
export const useClinicalPharmacyInterventionList = (options = {}) =>
  useGet([QUERY_KEY], ENDPOINTS.CLINICAL_PHARMACY_INTERVENTIONS.LIST, options);

// mutate(formData) — pass the form's `formData` object (INITIAL_FORM_DATA shape)
// straight through, plus draftStatus/attachmentIdsJson.
export const useSubmitClinicalPharmacyIntervention = (options = {}) => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload) => {
      const response = await apiClient.post(ENDPOINTS.CLINICAL_PHARMACY_INTERVENTIONS.SUBMIT, payload).catch(handleError);
      return handleSuccess(response);
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [QUERY_KEY] }),
    ...options,
  });
};
```

### `endpoints.js` addition
```js
CLINICAL_PHARMACY_INTERVENTIONS: {
  LIST:   '/clinical-pharmacy-interventions',  // GET  Admin
  SUBMIT: '/clinical-pharmacy-interventions',  // POST all roles
},
```

### Wiring notes
- The form's `formData` object's keys already match the API's request body field-for-field
  (both were built from the same `INITIAL_FORM_DATA` source) — the mutation payload can be
  `{ ...formData, draftStatus, attachmentIdsJson: JSON.stringify(files.map(f => f.id)) }`
  with minimal reshaping.
- `estimatedSaving` is a free-text string in the form (`timeSpent`/`estimatedSaving` fields
  are plain `<Input>`s) — convert to a number (or `null` if empty) before sending, since the
  backend column is `decimal?`.

---

## 4. Quality Project Tracker

**Replaces:** `QualityProjectTracker.jsx`'s `localStorage` read/write (the `RECORDS_KEY`
array) and `QualityProjectDashboard.jsx`'s `localStorage` read. `ProjectCharterTab.jsx`,
`PDSACycleTab.jsx`, `ProgressHistoryTab.jsx` need **no changes** — they only read/write the
`charter`/`cycles` props already passed down from `QualityProjectTracker.jsx`.

### Endpoints

**`POST /api/quality-projects`** — create the charter.
Request: every `emptyCharter()` field except `id`/`createdAt` (`projectTitle`, `orgName`,
`sponsorName`, `aimStatement`, `problem`, `reason`, `outcomes`, `outcomeMeasures`,
`processMeasures`, `initialActivities`, `barriers`, `stakeholders`, `status`).
Response: the full `QualityProjectDto` — same fields plus `id`, `achievements: null`,
`progressNotes: null`, `createdAt`, `cycles: []`.

**`GET /api/quality-projects`** — Admin only, dashboard list. Array of the same shape
(each with its `cycles` array populated).

**`GET /api/quality-projects/{id}`** — any logged-in user. Loads one project by id — this
is the one module that genuinely needs a detail-by-id call, since the tracker page opens
directly via `/quality-project-tracker/:id`.

**`PUT /api/quality-projects/{id}`** — updates the whole charter, **including**
`achievements`/`progressNotes` (the "Progress History" tab's own two fields — there's no
separate progress-history table or endpoint; that tab just edits these two fields on this
same record). Request: same shape as create plus `achievements`, `progressNotes`.

**`POST /api/quality-projects/{id}/cycles`** — add **or** edit one PDSA cycle (one upsert
endpoint for both, matching `PDSACycleTab.jsx`'s own save button, which already treats
add/edit as the same action). Request:
```json
{
  "id": 0,
  "changeIdea": "Add a checklist for PharmD orientation",
  "testerJson": "[\"Dr. Ahmed\",\"Jane Nurse\"]",
  "timeframe": "2026-09-15",
  "location": "Ward 4",
  "participantsJson": "[\"Student cohort A\"]",
  "learningGoal": "Reduce onboarding time",
  "predictionsJson": "[{\"prediction\":\"Faster onboarding\",\"data\":\"Time-to-competency\"}]",
  "observations": "",
  "studyResults": "",
  "studyLearning": "",
  "actPlan": "",
  "decision": null,
  "status": "In Progress"
}
```
`id: 0` (or omitted) = create a new cycle; pass an existing cycle's `id` to update it
instead. Response: the saved `QualityProjectCycleDto`.

### `src/API/qualityProjectApi.js` (new file)
```js
import { useMutation, useQueryClient } from '@tanstack/react-query';
import apiClient from './apiClient';
import { handleSuccess, handleError } from './apiUtils';
import { useGet } from './apiService';
import { ENDPOINTS } from './endpoints';

const QUERY_KEY = 'qualityProjects';

// GET /api/quality-projects — Admin only, dashboard list.
export const useQualityProjectList = (options = {}) =>
  useGet([QUERY_KEY], ENDPOINTS.QUALITY_PROJECTS.LIST, options);

// GET /api/quality-projects/{id} — one project (with its cycles), for the tracker page.
export const useQualityProject = (id, options = {}) =>
  useGet([QUERY_KEY, id], ENDPOINTS.QUALITY_PROJECTS.DETAIL(id), { enabled: !!id, ...options });

// mutate(charterFields) — create.
export const useCreateQualityProject = (options = {}) => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload) => {
      const response = await apiClient.post(ENDPOINTS.QUALITY_PROJECTS.CREATE, payload).catch(handleError);
      return handleSuccess(response);
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [QUERY_KEY] }),
    ...options,
  });
};

// mutate({ id, ...charterFields }) — update (incl. achievements/progressNotes).
export const useUpdateQualityProject = (options = {}) => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, ...payload }) => {
      const response = await apiClient.put(ENDPOINTS.QUALITY_PROJECTS.UPDATE(id), payload).catch(handleError);
      return handleSuccess(response);
    },
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: [QUERY_KEY] });
      queryClient.invalidateQueries({ queryKey: [QUERY_KEY, id] });
    },
    ...options,
  });
};

// mutate({ projectId, ...cycleFields }) — add or edit one PDSA cycle (cycleFields.id: 0 = new).
export const useUpsertQualityProjectCycle = (options = {}) => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ projectId, ...payload }) => {
      const response = await apiClient.post(ENDPOINTS.QUALITY_PROJECTS.UPSERT_CYCLE(projectId), payload).catch(handleError);
      return handleSuccess(response);
    },
    onSuccess: (_, { projectId }) => queryClient.invalidateQueries({ queryKey: [QUERY_KEY, projectId] }),
    ...options,
  });
};
```

### `endpoints.js` addition
```js
QUALITY_PROJECTS: {
  LIST:         '/quality-projects',                       // GET  Admin
  DETAIL:       (id) => `/quality-projects/${id}`,          // GET  all roles
  CREATE:       '/quality-projects',                        // POST all roles
  UPDATE:       (id) => `/quality-projects/${id}`,           // PUT  all roles
  UPSERT_CYCLE: (id) => `/quality-projects/${id}/cycles`,    // POST all roles
},
```

### Wiring notes
- This is the file needing the most rework: `QualityProjectTracker.jsx` currently does its
  own `localStorage`-array find/splice for load-by-id and save. Replace with
  `useQualityProject(id)` for the initial load (when `id` is present in the URL),
  `useCreateQualityProject()` for a brand-new project (no `id` yet), and
  `useUpdateQualityProject()`/`useUpsertQualityProjectCycle()` for subsequent saves from
  `ProjectCharterTab.jsx`/`PDSACycleTab.jsx`/`ProgressHistoryTab.jsx` (those tabs call back
  up to `QualityProjectTracker.jsx`'s own save handlers via the props they already receive
  — only those handlers' *implementation* changes, not the tabs).
- `tester`/`participants` (antd `Select mode="tags"`, arrays) and `predictions` (array of
  `{prediction, data}`) need `JSON.stringify(...)`/`JSON.parse(...)` at the API boundary,
  same pattern as Student Feedback's rating objects.

---

## 5. Research and Publications

**Replaces:** `ResearchPublications.jsx`'s `useState(initialPublications)` array and its
local add/edit/delete handlers, plus the separate `orcid` `useState`. **The DOI/PubMed
lookup code (`MOCK_DOI_DB`/`MOCK_PMID_DB`, `lookupDoi`/`lookupPmid`) stays completely
untouched** — it's a frontend-only demo, not part of this backend.

### Endpoints

**`GET /api/research-publications`** — any logged-in user. Array, newest first.

**`POST /api/research-publications`** — create.
```json
{
  "title": "Management of patients with cardiovascular disease...",
  "metaLine": "Journal article • Published research",
  "publicationType": "Journal Article",
  "publicationDate": "24 Aug 2026",
  "journal": "International Medical Journal",
  "authors": "Author Name, Co-authors",
  "description": null,
  "identifier": null,
  "source": null,
  "status": "Published",
  "tagsJson": "[\"Cardiovascular\",\"Research\"]"
}
```
Response: same fields plus `id`, `createdAt`.

**`PUT /api/research-publications/{id}`** — edit (same body shape as create).

**`DELETE /api/research-publications/{id}`** — the UI already supports multi-select +
delete; call once per selected id (or loop client-side — there's no bulk-delete endpoint).

**`GET /api/research-publications/orcid`** — current user's own. Response: `{ "orcid": "0000-0000-0000-0000" }` (empty string if never saved).

**`PUT /api/research-publications/orcid`** — save. Request: `{ "orcid": "0000-0000-0000-0000" }`.

### `src/API/researchPublicationApi.js` (new file)
```js
import { useMutation, useQueryClient } from '@tanstack/react-query';
import apiClient from './apiClient';
import { handleSuccess, handleError } from './apiUtils';
import { useGet } from './apiService';
import { ENDPOINTS } from './endpoints';

const QUERY_KEY = 'researchPublications';
const ORCID_KEY = 'researcherOrcid';

export const useResearchPublicationList = (options = {}) =>
  useGet([QUERY_KEY], ENDPOINTS.RESEARCH_PUBLICATIONS.LIST, options);

export const useOrcid = (options = {}) =>
  useGet([ORCID_KEY], ENDPOINTS.RESEARCH_PUBLICATIONS.ORCID, options);

export const useCreateResearchPublication = (options = {}) => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload) => {
      const response = await apiClient.post(ENDPOINTS.RESEARCH_PUBLICATIONS.CREATE, payload).catch(handleError);
      return handleSuccess(response);
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [QUERY_KEY] }),
    ...options,
  });
};

export const useUpdateResearchPublication = (options = {}) => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, ...payload }) => {
      const response = await apiClient.put(ENDPOINTS.RESEARCH_PUBLICATIONS.UPDATE(id), payload).catch(handleError);
      return handleSuccess(response);
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [QUERY_KEY] }),
    ...options,
  });
};

export const useDeleteResearchPublication = (options = {}) => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id) => {
      const response = await apiClient.delete(ENDPOINTS.RESEARCH_PUBLICATIONS.DELETE(id)).catch(handleError);
      return handleSuccess(response);
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [QUERY_KEY] }),
    ...options,
  });
};

export const useSaveOrcid = (options = {}) => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (orcid) => {
      const response = await apiClient.put(ENDPOINTS.RESEARCH_PUBLICATIONS.ORCID, { orcid }).catch(handleError);
      return handleSuccess(response);
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [ORCID_KEY] }),
    ...options,
  });
};
```

### `endpoints.js` addition
```js
RESEARCH_PUBLICATIONS: {
  LIST:   '/research-publications',               // GET    all roles
  CREATE: '/research-publications',               // POST   all roles
  UPDATE: (id) => `/research-publications/${id}`, // PUT    all roles
  DELETE: (id) => `/research-publications/${id}`, // DELETE all roles
  ORCID:  '/research-publications/orcid',         // GET/PUT all roles — current user's own
},
```

### Wiring notes
- `tags` array needs `JSON.stringify(...)`/`JSON.parse(...)` at the API boundary, same
  pattern as the other modules' array fields.
- `addDoiResult()`/`addPmidResult()` (the "add from lookup" buttons) should call
  `useCreateResearchPublication().mutate(...)` with the mock lookup's result fields — the
  lookup itself stays mock, but "adding" that result becomes a real save.
- `hasAddAnother` is UI-only (whether to show the "Add another" button) — not a backend
  field, don't send it.

---

## One thing NOT covered here — flag to the backend dev, not something to fix on the frontend

While generating the migration for these 5 modules, `dotnet ef migrations add` surfaced a
**pre-existing, unrelated** model/database drift: the C# model already has
`Permissions`/`SystemModules`/`RolePermissions` seed data for a "Notifications" and "System
Settings" (email settings) module that has no migration in source control yet, and the
local dev DB already has a *different* row at `Permissions.Id = 28` than what the model
expects there. This has nothing to do with these 5 modules — it was deliberately left out
of `AddNewModulesBackend`'s migration so that migration only does what it says. Someone
needs to reconcile that separately (regenerate a dedicated migration for it, resolving the
`Id = 28` conflict first) before it'll apply cleanly.
