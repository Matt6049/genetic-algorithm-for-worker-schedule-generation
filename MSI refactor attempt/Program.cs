﻿using System.ComponentModel;

namespace MSI_refactor_attempt {
    internal class Program {
        public const int GENERATIONS_COUNT = 100;
        const int SCHEDULE_COUNT = 500;
        const int PARENT_COUNT = 3;
        const double ELITISM_RATIO = 0.05;

        public static int currentGeneration = 0;
        static Schedule[] currentPopulation;
        static Schedule[] children;
        static Random rand;
        static int elitismCarryoverCount;

        static void PrintPopulation() {
            double max = currentPopulation.Max(Sched => Sched.CalculateScheduleFitness());
            double avg = currentPopulation.Average(Sched => Sched.CalculateScheduleFitness());

            Console.WriteLine($"Generacja: {currentGeneration}, Fitness maksymalny: {max}, Fitness średni: {avg}");
            //Console.WriteLine($"Current generation: {currentGeneration}");
            //foreach(Schedule sched in currentPopulation) {
            //    Console.WriteLine(sched);
            //}
            //Console.WriteLine();
        }

        static void Main(string[] args) {
            DateTime time1 = DateTime.Now;
            rand = new();
            currentPopulation = new Schedule[SCHEDULE_COUNT];
            elitismCarryoverCount = (int)Math.Ceiling(Schedule.WORKER_COUNT * ELITISM_RATIO);

            for(int i=0; i<SCHEDULE_COUNT; i++) {
                currentPopulation[i] = new();
                currentPopulation[i].RandomizeWorkers();
            }


            while(currentGeneration < GENERATIONS_COUNT) {
                PrintPopulation();
                children = new Schedule[SCHEDULE_COUNT];
                Schedule[] bestParents = currentPopulation.OrderBy(sched => sched.CalculateScheduleFitness()).TakeLast(elitismCarryoverCount).ToArray();

                
                for(int i=0; i<elitismCarryoverCount; i++) {
                    children[i] = bestParents[i];
                }

                for(int i=elitismCarryoverCount; i<SCHEDULE_COUNT; i++) {
                    List<int> candidates = Enumerable.Range(0, SCHEDULE_COUNT).ToList();
                    Schedule[] parents = new Schedule[PARENT_COUNT];
                    for(int j = 0; j < PARENT_COUNT; j++) {
                        int candidate = candidates[rand.Next(candidates.Count)];
                        candidates.Remove(candidate);
                        parents[j] = currentPopulation[candidate];
                    }
                    children[i] = new(parents);
                }
                currentPopulation = children;
                currentGeneration++;
            }
            DateTime time2 = DateTime.Now;
            Console.WriteLine($"Czas: {time2-time1}");
        }
    }
}
