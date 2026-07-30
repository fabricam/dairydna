# Learning dairy decision-making with DairyDNA

A guided tour of the DairyDNA app for people who are new to the dairy business.
Each page teaches a real planning problem that milk and cream supply chains face,
using a **synthetic** (made-up but realistic) U.S. network so you can explore
safely.

> **Honesty first.** Nothing here is live market advice, real truck dispatch, or
> a production system. Numbers are labeled **Synthetic**, **Public**,
> **Forecast**, **Recommendation**, or **Replay** so you always know what kind
> of signal you are looking at. Full boundary:
> [`honesty-boundary.md`](./honesty-boundary.md).

**How to start the app:** [`presenter-script.md`](./presenter-script.md) or
`./scripts/demo-start.ps1`. On Demo home, generate with profile `thin-slice`
and seed `104729` so your screens match this guide.

---

## The story in one paragraph

Dairy companies move perishable milk and cream from **farms** through
**facilities** (plants and warehouses) to **customers** (retailers, foodservice,
manufacturers). Milk spoils on a clock. Trucks have limited capacity. Fuel costs
money. Customer orders and market prices change. DairyDNA walks you through the
same loop a planner uses: build a picture of the network → see today’s stock and
risk → forecast what will be available, wanted, and worth → recommend feasible
shipments that protect margin → stress-test “what if?” → check that forecast
models are governed → replay past days without cheating with future information.

```text
Farms produce → Facilities store/process → Trucks haul → Customers buy
        ↑________________ planning & optimization ________________↑
```

---

## Dairy vocabulary used in the app

| Term | Plain meaning |
|------|----------------|
| **Farm** | Where cows produce raw milk. Herd size hints at supply volume. |
| **Facility** | Plant or warehouse that holds milk/cream (storage capacity in pounds). |
| **Customer** | Buyer with open orders and a location that affects haul cost. |
| **Product** | e.g. `RAW_MILK`, `CREAM` — each has a **max age (hours)** before it is too old to ship. |
| **Inventory lot** | A batch of product at a facility with quantity (lb) and an expiry. |
| **Open demand / order** | A committed customer request for a product on a date at a price ($/lb). |
| **As-of date** | “Pretend today is this date” — only data known by then is allowed. |
| **Contribution margin** | Roughly **revenue − transport cost** for a recommended move. Higher is better. |
| **Feasible** | The plan respects capacity, product compatibility, and other constraints. |
| **WAPE** | Weighted Absolute Percentage Error — how wrong a forecast was vs actuals. Lower is better. |
| **Leakage** | Accidentally using future data when planning a past day. Replay audits against this. |

Label colors / badges in the UI:

| Label | Trust it as… |
|-------|----------------|
| **Synthetic** | Demo-generated world (farms, stock, orders). |
| **Public** | External reference series (fixture imports), not a DairyDNA prediction. |
| **Forecast** | Statistical estimate with uncertainty — not a guarantee. |
| **Recommendation** | Optimizer suggestion — not a dispatched truck. |
| **Replay** | Historical re-plan for an as-of day. |
| **Not recommended** | Scenario simulation output — do not treat as an ops ship order. |

---

## Suggested learning path

| Step | Page | Route | What you learn |
|------|------|-------|----------------|
| 1 | Demo home | `/` or `/demo` | Create the synthetic dairy world |
| 2 | Network | `/network` | Geography of farms → plants → customers |
| 3 | Reference | `/reference` | Master data behind the map dots |
| 4 | Dashboard | `/dashboard` | Day-of stock, spoilage risk, demand, fleet |
| 5 | Facility detail | `/dashboard/facility/{id}` | One plant’s cooler |
| 6 | Imports | `/imports` | Public market observations vs synthetic |
| 7 | Supply forecasts | `/forecasts/supply` | How much product may be available |
| 8 | Demand forecasts | `/forecasts/demand` | How much customers may want |
| 9 | Price forecasts | `/forecasts/price` | What product may be worth |
| 10 | Recommendations | `/recommendations` | Which loads to move for margin |
| 11 | Scenarios | `/scenarios` | What-if stress tests |
| 12 | Models | `/models` | Forecast model lifecycle & quality |
| 13 | Replay | `/replay` | History without leakage; regret vs naive policies |

Shared visuals you will see repeatedly: **Network map**, **Inventory age chart**,
**Price sparkline**, **Forecast band chart**, **Margin & cost chart**.

---

## 1. Demo home — build the world

**Route:** `/` · `/demo`  
**Problem being solved:** Real dairy data is confidential. Planners still need a
full network to practice. This page **generates a reproducible synthetic dairy
economy** and optionally runs the first allocation.

### What to do
1. Leave **Generation profile** on `thin-slice` and **Seed** on `104729`.
2. Click **Generate synthetic data**.
3. Click **Load demo summary**.
4. Optionally **Run optimization**, then **View recommendations**.

### Page components

| Component | What it teaches |
|-----------|-----------------|
| Honesty banner | Demo ≠ production advice. |
| Profile + Seed | Same seed → same world (science of reproducibility). |
| Custom farm/facility/customer/truck counts | Network *scale* changes planning difficulty. |
| Generation status (id, profile, planning date) | Every later page hangs off a **generation id**. |
| **Load validation report** | Data quality: missingness %, seasonality — dirty inputs ruin plans. |
| Operational cards: Inventory / Open demand / Fleet | Three pillars of a morning ops huddle. |
| **Network map** (`NetworkMap`) | Who sits where on a schematic U.S. outline. |
| Inventory table (Facility, Product, Qty, Oldest expiry) | Stock is perishable — watch expiry. |
| Demand table (Customer, Product, Qty, Price/lb) | Money is in fulfilled orders at a price. |

### Industry takeaway
Before forecasting or optimizing, a dairy business must know **what it has**,
**who wants it**, and **what can haul it** — on a named planning day.

---

## 2. Network — see the supply chain geography

**Route:** `/network`  
**Problem being solved:** Milk does not teleport. Distance drives diesel cost and
delivery feasibility. This page is a **map of the physical network**.

### Page components

| Component | What it teaches |
|-----------|-----------------|
| `NetworkMap` | Farms (green), facilities, customers on a schematic contiguous U.S. |
| Legend | Three node types — never confuse a plant with a buyer. |
| **Refresh** | Reload points for the active generation. |

### Industry takeaway
Location is an economic input. Distant high-price customers can still lose to
nearby lower-price ones after transport cost — a theme that returns in
Scenarios and Recommendations.

---

## 3. Reference — master data behind the dots

**Route:** `/reference`  
**Problem being solved:** Maps are not enough. Ops systems need **stable
reference data**: products’ shelf life, truck compatibility, contracts.

### Page components

| Tab / control | What it teaches |
|---------------|-----------------|
| **farms** | Name, region, **herd** (proxy for supply potential), Active. |
| **facilities** | Type (plant vs warehouse mindset), region. |
| **customers** | Who can place demand. |
| **products** | Codes like `RAW_MILK` / `CREAM`; **Max age (h)** = shelf-life policy. |
| **trucks** | Capacity (lb) and product **compat** — not every truck hauls cream. |
| **contracts** | Time-bounded customer–product agreements. |
| **Include inactive** / **Deactivate** | Soft remove capacity from the network (a plant going offline). |

### Industry takeaway
Shelf life and truck compatibility are hard constraints. Soft-deactivating a
facility is how you model a temporary plant outage before you even touch the
optimizer.

---

## 4. Dashboard — the morning ops view

**Route:** `/dashboard`  
**Problem being solved:** “As of this morning, what do we hold, what is about to
spoil, who still needs product, and which trucks are free?”

### Page components

| Component | What it teaches |
|-----------|-----------------|
| Generation picker + **As-of (optional)** | Time-travel the same dataset to another day. |
| **Network map** + facility links | Jump from geography into a plant. |
| **Inventory age / risk** (`InventoryAgeChart`) | Lots bucketed by age with risk colors (Critical → lower). Spoilage pressure. |
| **Prices** (`PriceSparkline`) | Recent $/lb path for `RAW_MILK`; **Synthetic** vs **Public** series. |
| Inventory table (Days to expiry) | Perishability in numbers. |
| Open demand table | Committed asks still waiting. |
| Fleet table | Capacity and status of trucks. |

### Industry takeaway
Aging inventory near expiry is the clock the optimizer races against. Public
price ticks on the sparkline are **observations**, not DairyDNA’s forecast —
that distinction matters legally and analytically.

---

## 5. Facility detail — one plant’s cooler

**Route:** `/dashboard/facility/{FacilityId}`  
**Problem being solved:** Network totals hide local bottlenecks. Planners drill
into **one facility’s storage and lots**.

### Page components

| Component | What it teaches |
|-----------|-----------------|
| Facility header (type, region, classification) | Context for the plant. |
| Milk / cream storage capacity (lb) | Physical limits on what can sit overnight. |
| Inventory at as-of (Product, Qty, Days to expiry) | What is actually in the cooler today. |

### Industry takeaway
A facility can look “fine” in aggregate while one product is days from expiry
and another has unused tank space — allocation happens at this grain.

---

## 6. Imports — public data vs your world

**Route:** `/imports`  
**Problem being solved:** Dairies watch published market series (government or
exchange-style references). This page **imports fixture “public” observations**
so you can practice separating **Public** from **Synthetic** and **Forecast**.

### Page components

| Component | What it teaches |
|-----------|-----------------|
| Sources table + **Import fixture** | Where an external series comes from. |
| Last run (canonical rows, quarantine, checksum) | Good rows vs rejected junk; integrity via checksum. |
| Recent runs | Audit history of imports. |

### Industry takeaway
External data is valuable *and* messy. Quarantine exists because bad ticks
should not silently poison planning. Public series still are **not** trading
signals from DairyDNA.

---

## 7. Supply forecasts — will we have enough?

**Route:** `/forecasts/supply`  
**Problem being solved:** Herd output and plant receipts wobble with weather,
season, and operations. Planners need **estimated future availability** with
honest uncertainty.

### Page components

| Component | What it teaches |
|-----------|-----------------|
| **Train & publish** | Fit an ML.NET model on the synthetic history for this generation. |
| Model card strip (algorithm, Meets bar) | Did quality gates pass? |
| Facility picker + map | Forecasts are often **facility-level**. |
| **Forecast bands** (`ForecastBandChart`) | Solid **Actual**, shaded band (lower–upper), dashed **point** estimate. |

### Industry takeaway
A point forecast without a band hides risk. Dairy planning cares about
shortfalls as much as averages — under-supply forces expensive spot buys or
lost sales.

---

## 8. Demand forecasts — will they want it?

**Route:** `/forecasts/demand`  
**Problem being solved:** Confusing **open orders** (already committed) with
**forecast demand** (expected future asks) causes either stockouts or waste.

### Page components

| Component | What it teaches |
|-----------|-----------------|
| Honesty copy separating Open orders vs Forecast | Two different planning inputs. |
| **Train & publish** | Customer-oriented demand model. |
| Customer picker | Demand is not one national number — it is per buyer. |
| Forecast bands | History of orders/asks plus projected band. |

### Industry takeaway
You fulfill open orders first; you use demand forecasts to position inventory
and negotiate. Mixing them up is a classic planning error.

---

## 9. Price forecasts — what is it worth?

**Route:** `/forecasts/price`  
**Problem being solved:** Margin = price × quantity − cost. If tomorrow’s $/lb
moves, the “best” customer today may not be best tomorrow.

### Page components

| Component | What it teaches |
|-----------|-----------------|
| Product selector (`RAW_MILK`, `CREAM`) | Different products, different price paths. |
| **Train & publish** / **Load forecasts** | Build or view the series. |
| Forecast bands | Actual recorded prices vs projected band — **not a trade quote**. |

### Industry takeaway
Price uncertainty changes allocation. Optimizers can use spot prices or
forecast points/bands (price modes) — always read the label so you know which
world you optimized in.

---

## 10. Recommendations — what should we move?

**Route:** `/recommendations`  
**Problem being solved:** Given stock, trucks, distances, and orders, **which
feasible loads maximize contribution margin?**

Generate a run from Demo home (**Run optimization**), then open this page.

### Page components

| Component | What it teaches |
|-----------|-----------------|
| Status / Objective / Optimizer / Solve time | Feasible vs not; objective ≈ total contribution margin; default engine `ortools-cm-v1`. |
| teal **Recommendation** label | Suggestion only — nothing dispatched. |
| **Recommended flows map** | Same network with **lane arcs** facility → customer. |
| **Margin and cost breakdown** (`MarginCostChart`) | Per movement: **Revenue**, **Transport**, **Margin**. |
| Movements table (#, Qty, Revenue, Transport, Margin, Explanation) | Quantities plus **why** (constraints / rationale text). |

### Industry takeaway
A high offered price is useless if the truck cannot reach the customer before
expiry or within capacity. Feasibility and margin travel together. Transport
cost (fuel, miles, empty return in the costing model) is a first-class dairy
P&L line, not an afterthought.

---

## 11. Scenarios — what if the world changes?

**Route:** `/scenarios`  
**Problem being solved:** Diesel spikes, a plant loses capacity, demand surges,
or a distant customer raises price. Planners need **controlled what-if overlays**
without mutating the source dataset.

### Page components

| Component | What it teaches |
|-----------|-----------------|
| Warning: simulation ≠ operational recommendation | Critical honesty for interviews and demos. |
| **Apply flagship pack** | Four canned stories: `diesel-rise`, `distant-high-price`, `capacity-loss`, `demand-spike`. |
| **Run base optimization** / **Run scenario** | Compare baseline plan vs stressed plan. |
| Overrides JSON on definitions | Transparent knobs (fuel $/gal, demand scale, capacity scale, price bumps). |
| Base vs scenario bars (Objective, Unserved rows) | Did margin rise? Did unmet demand worsen? |
| **Not recommended** badge when applicable | Simulation output must not be sold as a ship order. |

### Industry takeaway
Scenario planning is how dairy commercial and logistics teams rehearse shocks.
Unserved demand is as important as objective value — leaving customers short
has relationship and contract costs the demo only proxies.

---

## 12. Models — govern the forecast engines

**Route:** `/models`  
**Problem being solved:** Silent model swaps mid-demo (or mid-week in a real
plant) destroy trust. Governance tracks **versions, quality, publish/retire,
and audit**.

### Page components

| Component | What it teaches |
|-----------|-----------------|
| Family filter (Supply / Demand / Price) | Three forecast families, one registry UX. |
| Registry table (algorithm, lifecycle, Meets bar, checksum) | Candidate → Published → Retired; checksum binds the artifact. |
| **Publish** / **Retire** (actor, reason, optional quality override) | Human-in-the-loop with an audit trail. |
| **Model card** | Intent, data summary, limitations (“not production advice”), leakage-control statement. |
| **Model vs baseline WAPE** chart | Did we beat a naive baseline? |
| Audit trail | Who changed lifecycle, when, and why. |

### Industry takeaway
In dairy analytics (and any regulated-ish planning culture), **traceability**
matters: which model version produced the numbers that drove yesterday’s loads?

---

## 13. Replay — prove plans over history

**Route:** `/replay`  
**Problem being solved:** “Would our optimizer have beaten simple rules on past
days — using only information available **as of** those days?”

### Page components

| Component | What it teaches |
|-----------|-----------------|
| Dataset window + **As-of date** scrubber | Walk the calendar day by day. |
| **Run replay** | Freeze inputs as-of D, optimize, store a `ReplayRun`. |
| Leakage passed / FAILED | Guardrail against peeking at future lots, orders, or features. |
| Version stamps (optimizer, costing, model ids) | Full reproducibility metadata. |
| Network map + top recommendation highlights | What the plan looked like that day. |
| **Build regret report** | Compare optimizer vs **Nearest-customer** and **Highest-price-first** baselines. |
| Regret chart + day table (Win?) | On which days did optimization earn its keep? |

### Industry takeaway
Regret analysis is how you justify analytics investment: not “the model is
clever,” but “versus policies a dispatcher might already use, we left less
money on the table — without time-traveling.” Proxy economics here are
synthetic; real P&L backtests need real ledgers (out of scope).

---

## Shared visual components (cheat sheet)

| Component | Where you see it | Dairy question it answers |
|-----------|------------------|---------------------------|
| **NetworkMap** | Demo, Dashboard, Network, Supply picker, Recommendations, Replay | Who is where, and which lanes are recommended? |
| **InventoryAgeChart** | Dashboard | What is about to spoil? |
| **PriceSparkline** | Dashboard | How have recent prices moved (Synthetic vs Public)? |
| **ForecastBandChart** | Supply / Demand / Price forecast pages | What is the estimate, and how wide is the uncertainty? |
| **MarginCostChart** | Recommendations | Where does revenue go after haul cost? |

---

## A 45-minute self-study session

1. **Demo** — Generate `thin-slice` / `104729`. Load summary. Note oldest expiry on inventory.
2. **Network → Reference** — Open Products; read **Max age (h)**. Open Trucks; read capacity/compat.
3. **Dashboard** — Load the generation. Read Inventory age / risk. Open one facility.
4. **Imports** — Import one fixture; note Public label vs Synthetic on the dashboard sparkline later.
5. **Forecasts** — Train supply, then demand, then price. Read each band chart aloud: actual vs band vs point.
6. **Optimize** — From Demo, Run optimization → Recommendations. Explain one movement’s revenue vs transport vs margin.
7. **Scenarios** — Apply flagship pack; run `diesel-rise`; compare objective and unserved rows.
8. **Models** — Open a model card; find WAPE vs baseline and the limitations paragraph.
9. **Replay** — Scrub two days; run replay; build a short regret window; find a day the optimizer “wins.”

---

## How the pieces fit (decision loop)

```text
Reference data + Synthetic generation
        ↓
Dashboard (as-of stock, risk, demand, fleet)  ←── Public imports (optional texture)
        ↓
Supply / Demand / Price forecasts (governed on Models)
        ↓
Transportation costing + OR-Tools optimization
        ↓
Recommendations (feasible lanes + margin)
        ↓
Scenarios (what-if)     Replay (as-of history + regret)
```

That loop is the core of modern dairy **commercial logistics analytics**:
perishability, geography, uncertain futures, and constrained trucks — turned
into transparent, labeled decision support.

---

## Next reading

| Doc | Use when |
|-----|----------|
| [`presenter-script.md`](./presenter-script.md) | You are presenting live |
| [`seed-pack.md`](./seed-pack.md) | You need exact profile/seed/scenario names |
| [`hardening-notes.md`](./hardening-notes.md) | Ports, Docker, timings, known empty-recommendation caveat |
| [`honesty-boundary.md`](./honesty-boundary.md) | Scope and non-claims |
| [`specs/README.md`](../../specs/README.md) | Engineering feature history 000–013 |
