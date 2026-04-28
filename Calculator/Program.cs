using System;
using System.Collections.Generic;
using System.Linq;

namespace BgElectionCalculator
{
    class Program
    {
        class Entry
        {
            public string Name { get; set; }
            public double Percentage { get; set; } // Changed from Votes to Percentage
            public int Seats { get; set; }
            public double Remainder { get; set; }
            public bool IsEligibleParty { get; set; } // false for "Scattered" and "None"
        }

        static void Main()
        {
            // Initial mock data based on percentages summing to 100%
            var entries = new List<Entry>
            {
                new Entry { Name = "Radev", Percentage = 44.594, IsEligibleParty = true },
                new Entry { Name = "JERB", Percentage = 13.387, IsEligibleParty = true },
                new Entry { Name = "PP-DB", Percentage = 12.618, IsEligibleParty = true },
                new Entry { Name = "DPS", Percentage = 7.12, IsEligibleParty = true },
                new Entry { Name = "Revival", Percentage = 4.257, IsEligibleParty = true },
                new Entry { Name = "Other Parties (Scattered < 4%)", Percentage = 18.04, IsEligibleParty = false },
                new Entry { Name = "I don't support anyone", Percentage = 0.4, IsEligibleParty = false }
            };

            while (true)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("=== BULGARIAN ELECTION CALCULATOR (HARE-NIEMEYER) ===\n");
                Console.ResetColor();

                // 1. Calculate the totals and the 4% threshold
                double totalPercentageWithNone = entries.Sum(e => e.Percentage);
                double nonePercentage = entries.First(e => e.Name.StartsWith("I don't")).Percentage;

                // "I don't support anyone" is excluded from the 4% math
                double totalValidPartyPercentage = totalPercentageWithNone - nonePercentage;

                // A party must earn at least 4% of the VALID party base
                double thresholdPercentage = totalValidPartyPercentage * 0.04;

                // Show a warning if percentages don't add up closely to 100%
                if (Math.Abs(totalPercentageWithNone - 100.0) > 0.1)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[!] Warning: Total percentages sum to {totalPercentageWithNone:0.00}% (ideally should be 100%).");
                    Console.ResetColor();
                }

                Console.WriteLine($"Total Entered %:          {totalPercentageWithNone:0.00}%");
                Console.WriteLine($"Valid Party % (Math Base):{totalValidPartyPercentage:0.00}%");
                Console.WriteLine($"4% Threshold requires:    {thresholdPercentage:0.00}% of total entered\n");

                // 2. Distribute Seats
                CalculateSeats(entries, thresholdPercentage);

                // 3. Draw the UI Table
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"{"ID",-3} | {"Name",-30} | {"%",-10} | {"Seats",-5} | {"Remainder",-8}");
                Console.WriteLine(new string('-', 66));
                Console.ResetColor();

                for (int i = 0; i < entries.Count; i++)
                {
                    var e = entries[i];

                    bool passed = e.IsEligibleParty && e.Percentage >= thresholdPercentage;
                    string seatStr = passed ? e.Seats.ToString() : "-";
                    string remStr = passed ? e.Remainder.ToString("0.000") : "-";

                    if (passed) Console.ForegroundColor = ConsoleColor.Green;
                    else if (e.IsEligibleParty) Console.ForegroundColor = ConsoleColor.Red;
                    else Console.ForegroundColor = ConsoleColor.DarkGray;

                    Console.WriteLine($"{i + 1,-3} | {e.Name,-30} | {e.Percentage,-10:0.00} | {seatStr,-5} | {remStr}");
                    Console.ResetColor();
                }

                Console.WriteLine(new string('-', 66));

                // 4. Interaction Menu
                Console.WriteLine("Options:");
                Console.WriteLine("[1 - " + entries.Count + "] Edit percentage for an entry");
                Console.WriteLine("[A] Add new party");
                Console.WriteLine("[X] Exit");
                Console.Write("\nChoose an option: ");

                string input = Console.ReadLine()?.Trim().ToUpper();

                if (input == "X") break;

                if (input == "A")
                {
                    Console.Write("Enter party name: ");
                    string name = Console.ReadLine();
                    Console.Write("Enter percentage (e.g., 5.5): ");
                    // Using double.TryParse
                    if (double.TryParse(Console.ReadLine(), out double pct))
                    {
                        entries.Insert(entries.Count - 2, new Entry { Name = name, Percentage = pct, IsEligibleParty = true });
                    }
                    continue;
                }

                if (int.TryParse(input, out int id) && id >= 1 && id <= entries.Count)
                {
                    Console.Write($"Enter new percentage for {entries[id - 1].Name} (e.g., 12.5): ");
                    if (double.TryParse(Console.ReadLine(), out double newPct) && newPct >= 0)
                    {
                        entries[id - 1].Percentage = newPct;
                    }
                }
            }
        }

        static void CalculateSeats(List<Entry> entries, double thresholdPercentage)
        {
            // Reset state
            foreach (var e in entries) { e.Seats = 0; e.Remainder = 0; }

            // Filter parties that meet the dynamic 4% threshold
            var eligibleParties = entries.Where(e => e.IsEligibleParty && e.Percentage >= thresholdPercentage).ToList();
            double sumEligiblePercentage = eligibleParties.Sum(e => e.Percentage);

            if (sumEligiblePercentage <= 0) return;

            int totalSeats = 240;
            int allocatedSeats = 0;

            // Phase 1: Integer Allocation (The "Hare" part)
            foreach (var party in eligibleParties)
            {
                // The proportion is the party's % divided by the total % of all passing parties
                double exactSeats = (party.Percentage / sumEligiblePercentage) * totalSeats;
                party.Seats = (int)Math.Floor(exactSeats);
                party.Remainder = exactSeats - party.Seats;
                allocatedSeats += party.Seats;
            }

            // Phase 2: Remainder Distribution (The "Niemeyer" part)
            int remainingSeats = totalSeats - allocatedSeats;

            // Sort by largest remainder first
            var partiesOrderedByRemainder = eligibleParties.OrderByDescending(p => p.Remainder).ToList();

            for (int i = 0; i < remainingSeats && i < partiesOrderedByRemainder.Count; i++)
            {
                partiesOrderedByRemainder[i].Seats++;
            }
        }
    }
}