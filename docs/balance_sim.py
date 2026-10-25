"""Rough model of Dry Cell supply against use over a year, for checking docs/BALANCE.md.

Run: python docs/balance_sim.py

The flashlight runs on two matching cells used together. A pair of Dry Cells lasts 4 hours, so the model counts cells as two
hours of light each. Cells cost 15g.
Assumptions: the shop is open six days of seven, the player buys up to the carry limit on every open day, reloads from the
bag during the day, and needs the same number of hours of light every day. Crafting and Battery Packs are left out, so a shortfall here
is what a player has to cover by crafting or by packs.
"""
import random

HOURS_PER_CELL = 2   # two cells are used together and last 4 hours, so one cell is two hours of light
SLOTS = 2
DAILY_STOCK = 16
CARRY = 16
OUT_PCT = 15
LOW_PCT = 25


def year(hours_per_day, rng):
    stored = float(SLOTS + CARRY)   # cells in the flashlight and the bag, a part used cell counts as a part
    short_days = 0
    bought = 0
    for day in range(112):
        if day % 7 != 1:                       # the shop is closed one day a week
            if rng.random() * 100 < OUT_PCT:
                stock = 0
            elif rng.random() * 100 < LOW_PCT:
                stock = DAILY_STOCK // 2
            else:
                stock = DAILY_STOCK
            carried = max(0.0, stored - SLOTS)
            take = min(stock, max(0, int(CARRY - carried)))   # the shop counts whole cells in the bag
            stored += take
            bought += take
        need = hours_per_day / HOURS_PER_CELL
        if stored + 1e-9 < need:
            short_days += 1
            stored = 0.0
        else:
            stored -= need
    return short_days / 112 * 100, bought / 112


def main():
    rng = random.Random(7)
    print("hours of light a day -> share of days with too little light, cells bought a day")
    for hours in (4, 6, 8, 12, 16, 20):
        runs = [year(hours, rng) for _ in range(400)]
        short = sum(r[0] for r in runs) / len(runs)
        bought = sum(r[1] for r in runs) / len(runs)
        print(f"{hours:>2} h  {short:5.1f}% short   {bought:4.2f} cells a day   cost {bought * 15:5.0f}g a day")


if __name__ == "__main__":
    main()
