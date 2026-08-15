/* ==========================================================================
   Sample datasets — deterministic generators used by the Data Source view.
   ========================================================================== */

// ── Sample datasets ─────────────────────────────────────────────────────
export function buildSalesCsv() {
  const regions = ["North America", "Europe", "Asia Pacific"];
  const products = ["Alpha", "Beta", "Gamma"];
  let rows = ["Month,Region,Product,Revenue,Cost,Units"];
  for (let i = 0; i < 24; i++) {
    const d = new Date(Date.UTC(2023, i, 1));
    const region = regions[i % regions.length];
    const product = products[Math.floor(i / 2) % products.length];
    let base = 12000 + i * 650 + Math.sin(i / 2) * 1400;
    if (i === 15) base *= 3.1;
    const revenue = Math.round(base);
    const cost = Math.round(revenue * (0.55 + (i % 3) * 0.03));
    const units = Math.round(revenue / 95);
    rows.push([d.toISOString().slice(0, 10), region, product, revenue, cost, units].join(","));
  }
  return rows.join("\n");
}

// Small deterministic PRNG so the large samples are reproducible across reloads.
function mulberry32(seed) {
  return function () {
    seed |= 0; seed = (seed + 0x6D2B79F5) | 0;
    let t = Math.imul(seed ^ (seed >>> 15), 1 | seed);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

// ~5000 e-commerce orders across channels, categories and countries.
export function buildOrdersCsv() {
  const rnd = mulberry32(1337);
  const channels = ["Web", "Mobile App", "Marketplace", "Retail"];
  const categories = ["Electronics", "Apparel", "Home", "Beauty", "Sports", "Grocery"];
  const countries = ["USA", "UK", "Germany", "India", "Japan", "Brazil", "Canada"];
  const statuses = ["Delivered", "Shipped", "Processing", "Returned", "Cancelled"];
  const rows = ["OrderId,Date,Channel,Category,Country,Quantity,UnitPrice,Discount,Total,Status"];
  const start = Date.UTC(2024, 0, 1);
  for (let i = 0; i < 5000; i++) {
    const date = new Date(start + Math.floor(rnd() * 365) * 86400000).toISOString().slice(0, 10);
    const channel = channels[Math.floor(rnd() * channels.length)];
    const category = categories[Math.floor(rnd() * categories.length)];
    const country = countries[Math.floor(rnd() * countries.length)];
    const status = statuses[Math.floor(rnd() * statuses.length)];
    const qty = 1 + Math.floor(rnd() * 6);
    const unitPrice = Math.round((5 + rnd() * 495) * 100) / 100;
    const discount = Math.round(rnd() * 30);
    const total = Math.round(qty * unitPrice * (1 - discount / 100) * 100) / 100;
    rows.push([1000 + i, date, channel, category, country, qty, unitPrice, discount, total, status].join(","));
  }
  return rows.join("\n");
}

// ~5000 IoT sensor readings from a fleet of devices over time.
export function buildSensorsCsv() {
  const rnd = mulberry32(4242);
  const devices = ["dev-01", "dev-02", "dev-03", "dev-04", "dev-05", "dev-06", "dev-07", "dev-08"];
  const sites = ["Plant A", "Plant B", "Warehouse", "Datacenter"];
  const rows = ["Timestamp,Device,Site,TemperatureC,Humidity,VibrationG,Status"];
  let t = Date.UTC(2024, 5, 1, 0, 0, 0);
  for (let i = 0; i < 5000; i++) {
    t += 10 * 60000; // 10-minute cadence
    const device = devices[i % devices.length];
    const site = sites[i % sites.length];
    const temp = Math.round((20 + Math.sin(i / 40) * 6 + rnd() * 4) * 10) / 10;
    const humidity = Math.round(35 + rnd() * 40);
    const vibration = Math.round((0.2 + rnd() * 1.6) * 100) / 100;
    const status = temp > 30 || vibration > 1.6 ? "Alert" : "OK";
    rows.push([new Date(t).toISOString(), device, site, temp, humidity, vibration, status].join(","));
  }
  return rows.join("\n");
}

// ~5000 customer support tickets with priority, channel and resolution time.
export function buildTicketsCsv() {
  const rnd = mulberry32(9001);
  const categories = ["Billing", "Technical", "Account", "Shipping", "Feature Request", "Bug"];
  const priorities = ["Low", "Medium", "High", "Critical"];
  const channels = ["Email", "Chat", "Phone", "Social"];
  const agents = ["Ava", "Ben", "Chen", "Dara", "Eli", "Farah", "Gita", "Hugo"];
  const rows = ["TicketId,CreatedDate,Category,Priority,Channel,Agent,ResolutionHours,SatisfactionScore,Resolved"];
  const start = Date.UTC(2024, 0, 1);
  for (let i = 0; i < 5000; i++) {
    const date = new Date(start + Math.floor(rnd() * 365) * 86400000).toISOString().slice(0, 10);
    const category = categories[Math.floor(rnd() * categories.length)];
    const priority = priorities[Math.floor(rnd() * priorities.length)];
    const channel = channels[Math.floor(rnd() * channels.length)];
    const agent = agents[Math.floor(rnd() * agents.length)];
    const resolution = Math.round((0.5 + rnd() * 71.5) * 10) / 10;
    const satisfaction = 1 + Math.floor(rnd() * 5);
    const resolved = rnd() > 0.12 ? "Yes" : "No";
    rows.push([5000 + i, date, category, priority, channel, agent, resolution, satisfaction, resolved].join(","));
  }
  return rows.join("\n");
}

export const SAMPLES = {
  sales: { name: "Global Sales 2023–2024", format: "Csv", data: buildSalesCsv() },
  web:   { name: "Web Traffic", format: "Csv", data:
`Week,Channel,Sessions,Conversions
2024-W01,Organic,4200,180
2024-W01,Paid,3100,210
2024-W02,Organic,4550,201
2024-W02,Paid,2980,190
2024-W03,Organic,4810,220
2024-W03,Paid,3320,240
2024-W04,Organic,5010,244
2024-W04,Paid,3500,265
2024-W05,Organic,5320,270
2024-W05,Paid,3610,281
2024-W06,Organic,5590,299
2024-W06,Paid,3990,320` },
  hr: { name: "Headcount", format: "Json", data:
`[
  { "Department": "Engineering", "Headcount": 128, "AttritionRate": 0.07, "OpenRoles": 12 },
  { "Department": "Sales", "Headcount": 86, "AttritionRate": 0.14, "OpenRoles": 9 },
  { "Department": "Marketing", "Headcount": 34, "AttritionRate": 0.11, "OpenRoles": 3 },
  { "Department": "Support", "Headcount": 52, "AttritionRate": 0.18, "OpenRoles": 6 },
  { "Department": "Finance", "Headcount": 21, "AttritionRate": 0.05, "OpenRoles": 1 },
  { "Department": "Operations", "Headcount": 44, "AttritionRate": 0.09, "OpenRoles": 4 }
]` },
  orders:  { name: "E-commerce Orders (5k)", format: "Csv", data: buildOrdersCsv() },
  sensors: { name: "IoT Sensor Readings (5k)", format: "Csv", data: buildSensorsCsv() },
  tickets: { name: "Support Tickets (5k)", format: "Csv", data: buildTicketsCsv() },
};
