using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum BudgetCategory { Accommodation, Transportation, Food, Miscellaneous }

public class BudgetPlan
{
    public decimal TotalBudget { get; }
    public int TripDays { get; }
    public Dictionary<BudgetCategory, decimal> Minimums { get; } = new Dictionary<BudgetCategory, decimal>();
    public Dictionary<BudgetCategory, decimal> Weights { get; } = new Dictionary<BudgetCategory, decimal>();
    public Dictionary<BudgetCategory, decimal> Allocations { get; } = new Dictionary<BudgetCategory, decimal>();

    public BudgetPlan(decimal totalBudget, int tripDays)
    {
        if (totalBudget <= 0)
            throw new ArgumentException("Budget must be positive");
        if (tripDays <= 0)
            throw new ArgumentException("Trip duration must be at least 1 day");

        TotalBudget = totalBudget;
        TripDays = tripDays;

        foreach (BudgetCategory category in Enum.GetValues(typeof(BudgetCategory)))
        {
            Minimums[category] = 0;
            Weights[category] = 1; 
            Allocations[category] = 0;
        }
    }

    public void SetMinimum(BudgetCategory category, decimal minimum)
    {
        if (minimum < 0)
            throw new ArgumentException("Minimum cannot be negative");
        Minimums[category] = minimum;
    }

    public void SetWeight(BudgetCategory category, decimal weight)
    {
        if (weight < 0)
            throw new ArgumentException("Weight cannot be negative");
        Weights[category] = weight;
    }

    public void AllocateBudget()
    {
        decimal totalWeights = Weights.Values.Sum();
        decimal remainingBudget = TotalBudget;

        foreach (var category in Enum.GetValues(typeof(BudgetCategory)).Cast<BudgetCategory>())
        {
            decimal weightedAllocation = TotalBudget * Weights[category] / totalWeights;
            Allocations[category] = weightedAllocation;
            remainingBudget -= weightedAllocation;
        }

        var underMinimumCategories = new List<BudgetCategory>();
        foreach (var category in Enum.GetValues(typeof(BudgetCategory)).Cast<BudgetCategory>())
        {
            if (Allocations[category] < Minimums[category])
                underMinimumCategories.Add(category);
        }

        if (underMinimumCategories.Any())
        {
            RedistributeToMeetMinimums(underMinimumCategories);
        }
       
        if (Math.Abs(Allocations.Values.Sum() - TotalBudget) > 0.01m)
            throw new InvalidOperationException("Allocation error - totals don't match");
    }

    private void RedistributeToMeetMinimums(List<BudgetCategory> underMinimumCategories)
    {
        decimal totalDeficit = underMinimumCategories
            .Sum(c => Minimums[c] - Allocations[c]);

        var flexibleCategories = Enum.GetValues(typeof(BudgetCategory))
            .Cast<BudgetCategory>()
            .Where(c => !underMinimumCategories.Contains(c))
            .ToList();

        if (!flexibleCategories.Any())
            throw new InvalidOperationException("Cannot meet all minimum requirements with current budget");

        decimal totalFlexibleWeights = flexibleCategories.Sum(c => Weights[c]);

        foreach (var category in underMinimumCategories)
        {
            Allocations[category] = Minimums[category];
        }

        foreach (var category in flexibleCategories)
        {
            decimal reduction = totalDeficit * Weights[category] / totalFlexibleWeights;
            Allocations[category] -= reduction;

            if (Allocations[category] < 0)
                throw new InvalidOperationException("Cannot meet minimum requirements without negative allocation");
        }
    }

    public void PrintAllocation()
    {
        Console.WriteLine($"\nBudget Allocation for {TripDays} day trip (Total: {TotalBudget:C2})");
        Console.WriteLine(new string('=', 70));
        Console.WriteLine($"{"Category",-15} {"Total",10} {"Per Day",10} {"% Total",10} {"Min Req",10}");
        Console.WriteLine(new string('-', 70));

        foreach (var category in Enum.GetValues(typeof(BudgetCategory)).Cast<BudgetCategory>())
        {
            Console.WriteLine($"{category,-15} {Allocations[category],10:C2} " +
                            $"{Allocations[category] / TripDays,10:C2} " +
                            $"{Allocations[category] / TotalBudget,10:P1} " +
                            $"{Minimums[category],10:C2}");
        }

        Console.WriteLine(new string('-', 70));
        Console.WriteLine($"{"TOTAL",-15} {Allocations.Values.Sum(),10:C2} " +
                        $"{Allocations.Values.Sum() / TripDays,10:C2} " +
                        $"{1,10:P1}");
    }
}

namespace Travel_Planner_Budget_Split
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Travel Budget Planner");
            Console.WriteLine("Enter total budget:");
            decimal budget = decimal.Parse(Console.ReadLine());

            Console.WriteLine("Enter trip duration (days):");
            int days = int.Parse(Console.ReadLine());

            var plan = new BudgetPlan(budget, days);

            Console.WriteLine("\nSet minimum requirements (enter 0 if none):");
            foreach (BudgetCategory category in Enum.GetValues(typeof(BudgetCategory)))
            {
                Console.Write($"Minimum for {category}: ");
                decimal min = decimal.Parse(Console.ReadLine());
                plan.SetMinimum(category, min);
            }

            Console.WriteLine("\nSet priority weights (higher = more important):");
            decimal totalWeights = 0;
            foreach (BudgetCategory category in Enum.GetValues(typeof(BudgetCategory)))
            {
                Console.Write($"Weight for {category} (current total = {totalWeights}): ");
                decimal weight = decimal.Parse(Console.ReadLine());
                plan.SetWeight(category, weight);
                totalWeights += weight;
            }

            plan.AllocateBudget();
            plan.PrintAllocation();
        }
    }
}
