#!/usr/bin/env node
// Generate sample recipes across diverse themes for visual evidence
import { execSync } from 'child_process';

const themes = [
  'Quattro formaggi with four Italian cheeses',
  'Meat lovers with pepperoni, sausage, bacon',
  'Vegan supreme with mushrooms, peppers, olives',
  'Buffalo chicken with ranch drizzle',
  'Truffle mushroom with arugula',
  'Pepperoni and jalapeno with hot honey',
  'Pesto chicken with sun-dried tomatoes',
  'White pizza with ricotta and garlic',
  'BBQ pulled pork with red onions',
  'Dessert pizza with Nutella and strawberries'
];

for (const t of themes) {
  console.log(`\n=== ${t} ===`);
  try {
    const safe = t.toLowerCase().replace(/[^a-z0-9]+/g, '-');
    execSync(`node tools/integration-test.mjs --skip-verifier`, {
      stdio: 'inherit',
      env: { ...process.env, INTEGRATION_PROMPT: t }
    });
  } catch (e) {
    console.error('FAIL:', e.message?.slice(0, 200));
  }
}
