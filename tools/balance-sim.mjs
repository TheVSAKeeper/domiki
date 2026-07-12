// Симулятор баланса Domiki v2 (§8.7). Daily-step, аналитическая модель добычи.
// Модель uptime: длинный (24ч) dig -> трудяга почти всегда занят; простой ≈
// половина промежутка между заходами (24ч-рецепт финиширует и ждёт рестарта).
//   uptime = 1 - G/48,  G = 24/заходов.  casual 75% / optimal 90% / extreme 98%.
// Доход/трудяга-час: соло dig+sell = 9 монет (§4.1); группа глины = 15 (§8.3).
// Цель: подобрать стоимости так, чтобы engaged (optimal) брал ядро за ~1-2 нед.
// ВНИМАНИЕ: точные день-счётчики зависят от фикс-стратегии (breadth-first «макс
// всё») и не точны. Доверять структурным выводам и таблице resourceWait (§8.2),
// а не абсолютным дням. Запуск: node tools/balance-sim.mjs

const RES = { coin: 1, stone: 2, wood: 3, clay: 4, gold: 5 };
const TYPE = { forge: 1, barracks: 2, stone_mine: 3, gold_mine: 4, clay_mine: 5, lumber_mill: 6, market: 7 };
const MAXCOUNT = { 1: 1, 2: 5, 3: 2, 4: 2, 5: 2, 6: 2, 7: 1 };
const PLODDERS = { 0: 0, 1: 1, 2: 2, 3: 3, 4: 4, 5: 5 };
const SLOTS = { 0: 0, 1: 1, 2: 1, 3: 2, 4: 2, 5: 3 };
const UPG_H = { 1: 60 / 3600, 2: 300 / 3600, 3: 1, 4: 10, 5: 48 }; // часы

const PROPOSED = { // = миграция SeedStage0Balance
  1: { [RES.coin]: 20 },
  2: { [RES.coin]: 100 },
  3: { [RES.coin]: 300, [RES.stone]: 15, [RES.wood]: 15 },
  4: { [RES.coin]: 1000, [RES.stone]: 50, [RES.wood]: 50, [RES.clay]: 30 },
  5: { [RES.coin]: 6000, [RES.stone]: 150, [RES.wood]: 150, [RES.clay]: 100, [RES.gold]: 20 },
};
const CURRENT = { 1: { 1: 10 }, 2: { 1: 20 }, 3: { 1: 30 }, 4: { 1: 40 }, 5: { 1: 50 } };

const SELL = 10, SOLO = SELL /*10*/, GROUP_PER = (8 * SELL - 5) / 5 /*15*/;

const plan = (() => {
  const p = [];
  const buy = (t) => p.push({ t, up: false });
  const up = (t, i, l) => p.push({ t, i, up: true, l });
  buy(TYPE.market); buy(TYPE.barracks); buy(TYPE.clay_mine);
  buy(TYPE.barracks); buy(TYPE.stone_mine); buy(TYPE.lumber_mill);
  buy(TYPE.barracks); up(TYPE.clay_mine, 0, 2); buy(TYPE.barracks);
  buy(TYPE.gold_mine); buy(TYPE.barracks); buy(TYPE.clay_mine);
  for (let l = 2; l <= 5; l++) for (let i = 0; i < 5; i++) up(TYPE.barracks, i, l);
  for (let l = 2; l <= 5; l++) { up(TYPE.clay_mine, 0, l); up(TYPE.clay_mine, 1, l); up(TYPE.market, 0, l); up(TYPE.stone_mine, 0, l); up(TYPE.lumber_mill, 0, l); up(TYPE.gold_mine, 0, l); }
  return p;
})();

function sim(cost, sessionsPerDay) {
  const G = 24 / sessionsPerDay;
  const uptime = Math.max(0.5, 1 - G / 48);
  const S = { coin: 1000, res: { 2: 0, 3: 0, 4: 0, 5: 0 }, own: [] };
  const upg = new Map(); // индекс_в_own -> дней осталось
  let ptr = 0;
  const ms = {};
  const mark = (k, d) => { if (ms[k] === undefined) ms[k] = d; };

  const cnt = (t) => S.own.filter((o) => o.t === t).length;
  const insts = (t) => S.own.filter((o) => o.t === t);
  const plodders = () => insts(TYPE.barracks).reduce((s, o) => s + PLODDERS[o.level], 0);
  const slots = () => S.own.filter((o) => [3, 4, 5, 6].includes(o.t)).reduce((s, o) => s + SLOTS[o.level], 0);
  const group = () => insts(TYPE.clay_mine).some((o) => o.level >= 2);
  const hasMarket = () => insts(TYPE.market).some((o) => o.level >= 1);
  const afford = (c) => Object.entries(c).every(([r, q]) => r == 1 ? S.coin >= q : S.res[r] >= q);
  const pay = (c) => Object.entries(c).forEach(([r, q]) => { if (r == 1) S.coin -= q; else S.res[r] -= q; });

  const DAY = 1 / 24;
  for (let day = 0, tick = 0; day <= 400; day += DAY, tick++) {
    // апгрейды тикают в часах
    for (const [i, left] of [...upg]) { const nl = left - DAY; if (nl <= 0) { S.own[i].level += 1; upg.delete(i); } else upg.set(i, nl); }

    // производство за DAY (час)
    const P = plodders(), diggers = Math.min(P, slots());
    if (diggers > 0 && hasMarket()) {
      const ph = diggers * uptime * DAY * 24; // трудяга-часов продуктивных за DAY
      const a = plan[ptr];
      const c = a ? cost[a.up ? a.l : 1] : {};
      const need = {};
      for (const [r, q] of Object.entries(c)) if (r != 1 && S.res[r] < q) need[r] = q - S.res[r];
      let budget = ph; // трудяга-часов
      for (const r of Object.keys(need)) {
        if (!insts(mineFor(r)).some((o) => o.level >= 1)) continue;
        const take = Math.min(budget, need[r]); // 1 ресурс/трудяга-час
        S.res[r] += take; S.coin -= take; budget -= take;
        if (budget <= 0) break;
      }
      if (budget > 0) S.coin += budget * (group() && P >= 5 ? GROUP_PER : SOLO);
    }

    // трата
    let g = 0;
    while (plan[ptr] && g++ < 80) {
      const a = plan[ptr], c = cost[a.up ? a.l : 1];
      if (!afford(c)) break;
      if (a.up) {
        const inst = insts(a.t)[a.i];
        if (!inst || inst.level < a.l - 1 || [...upg.keys()].includes(S.own.indexOf(inst))) break;
        pay(c); upg.set(S.own.indexOf(inst), UPG_H[a.l] / 24);
        mark(`${tn(a.t)}[${a.i}]L${a.l}`, day + UPG_H[a.l] / 24);
      } else {
        if (cnt(a.t) >= MAXCOUNT[a.t]) { ptr++; continue; }
        pay(c); S.own.push({ t: a.t, level: 1 });
        mark(`buy ${tn(a.t)}#${cnt(a.t)}`, day);
      }
      ptr++;
    }
    if (ptr >= plan.length && upg.size === 0) { mark('DONE', day); break; }
    // маркеры ядра
    if (plodders() >= 25) mark('25plod', day);
    if (insts(TYPE.barracks).filter((o) => o.level >= 5).length >= 1) mark('barrackL5', day);
  }
  return { ms, uptime };
}
const mineFor = (r) => ({ 2: 3, 3: 6, 4: 5, 5: 4 }[r]);
const tn = (t) => Object.entries(TYPE).find(([, v]) => v === t)[0];

const SC = [['casual  2/д', 2], ['optimal 5/д', 5], ['extreme 24/д', 24]];
function report(label, cost) {
  console.log(`\n==== ${label} ====`);
  for (const [name, s] of SC) {
    const r = sim(cost, s);
    const k = (x) => r.ms[x] !== undefined ? `${r.ms[x].toFixed(1)}д` : '—';
    console.log(`  ${name}  idle=${((1 - r.uptime) * 100).toFixed(0)}%  барак→L5=${k('barrackL5')}  25труд=${k('25plod')}  ядро_DONE=${k('DONE')}`);
  }
}
report('CURRENT', CURRENT);
report('PROPOSED', PROPOSED);

// §8.2 resourceWait: cost_coin / доход_в_час vs 0.3-0.6 * таймер
console.log('\n==== §8.2 resourceWait (PROPOSED, доход при типичном для стадии числе трудяг) ====');
const stagePlod = { 2: 2, 3: 6, 4: 14, 5: 25 };
for (const l of [2, 3, 4, 5]) {
  const inc = stagePlod[l] * (l >= 3 ? GROUP_PER : SOLO); // монет/час village
  const wait = PROPOSED[l][1] / inc; // часов
  const timer = UPG_H[l];
  const tgtLo = timer * 0.3, tgtHi = timer * 0.6;
  const ok = wait >= tgtLo && wait <= tgtHi ? 'OK' : (wait < tgtLo ? 'дёшево' : 'дорого');
  console.log(`  L${l}: cost=${PROPOSED[l][1]}монет доход=${inc.toFixed(0)}/ч wait=${wait.toFixed(2)}ч таймер=${timer}ч цель=${tgtLo.toFixed(2)}-${tgtHi.toFixed(2)}ч -> ${ok}`);
}
