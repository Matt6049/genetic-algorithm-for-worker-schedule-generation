﻿using Newtonsoft.Json;
namespace MSI_refactor_attempt
{
    public class Worker {
        public const double MAX_FITNESS = 8;
        public const int MAX_WORKDAYS = 5;

        const double OFFDAY_WEIGHT = 0.5;
        const double OVERWORK_WEIGHT = 0.3;
        const double DISLIKED_DAY_WEIGHT = 0.2;

        const double MUTATION_CHANCE = 0.2;
        const int MAX_DISLIKED_DAYS = 3;
        const double DISLIKED_CHANCE = 0.65;
        const double OFFDAY_CHANCE = 0.2;

        static List<Preferences> PREFERENCES_LIST {get;}


        static Worker() {
            if (File.Exists("testy")) {
                string list = File.ReadAllText("testy");
                PREFERENCES_LIST = JsonConvert.DeserializeObject<List<Preferences>>(list);
                if (PREFERENCES_LIST.Count != Schedule.WORKER_COUNT) File.Delete("testy");
            }
            if (!File.Exists("testy")) {
                PREFERENCES_LIST = new();
                for (int i = 0; i < Schedule.WORKER_COUNT; i++) {
                    PREFERENCES_LIST.Add(new());
                }
                string s = JsonConvert.SerializeObject(PREFERENCES_LIST);
                File.WriteAllText("testy", s);
            }
        }

        
        public int PreferenceIndex { get; init; }
        public bool[] AssignedWorkdays { get; set; }
        public double fitness { get; private set; }
        double[] WorkdayFavorabilities { get; set; }
        int ShiftCount { get; set; }
        int DislikedShiftCount { get; set; }
        Random Rand { get; set; }
        Preferences PersonalPreference { get; set; }

        public Worker(int preferenceIndex, bool[] assignedWorkdays) {
            if (preferenceIndex >= Schedule.WORKER_COUNT) throw new Exception("Invalid preferenceIndex, above worker limit");
            this.PreferenceIndex = preferenceIndex;
            this.PersonalPreference = PREFERENCES_LIST[preferenceIndex];
            this.AssignedWorkdays = assignedWorkdays;
            this.Rand = new();
            RecountShifts();
            WorkdayFavorabilities = new double[Schedule.WEEKDAYS];
            RecalculateFavorability();
        }

        public bool AttemptMutation(int day) {
            if(Rand.NextDouble() < (1 - WorkdayFavorabilities[day])*MUTATION_CHANCE) {
                AssignedWorkdays[day] = !AssignedWorkdays[day];
                //okazja na poprawę: dodawanie lub odejmowanie z liczb zmian, nielubianych zmian itd zamiast przeliczania od nowa
                RecountShifts();
                RecalculateFavorability();
                return true;
            }
            return false;
        }

        void RecalculateFavorability() {
            this.fitness = 1;
            for (int day = 0; day < Schedule.WEEKDAYS; day++) {
                WorkdayFavorabilities[day] = FindFavorability(AssignedWorkdays[day], day);
                this.fitness += WorkdayFavorabilities[day] * (MAX_FITNESS-1) / Schedule.WEEKDAYS;
            }
        }

        double FindFavorability(bool proposedShiftState, int day) {

            double weight = 1;
            weight -= OffDayPenalty(day)*OFFDAY_WEIGHT;
            weight -= OverworkPenalty(day)*OVERWORK_WEIGHT;
            weight -= DislikedPenalty(day)*DISLIKED_DAY_WEIGHT;

            return proposedShiftState ? weight : 1 - weight;
        }


        double OffDayPenalty(int day) {
            if (PersonalPreference.OffDays.Contains(day)) {
                return 1 / PersonalPreference.OffDays.Length;
            }
            return 0;
        }


        double OverworkPenalty(int day) {
            if (AssignedWorkdays[day] && ShiftCount > MAX_WORKDAYS
                || !AssignedWorkdays[day] && ShiftCount+1>MAX_WORKDAYS) {
                return 1;
            }
            return 0;
        }

        double DislikedPenalty(int day) {
            if (PersonalPreference.DislikedWorkdays.Contains(day)) {
                return Math.Pow(8, (
                    AssignedWorkdays[day]? DislikedShiftCount : DislikedShiftCount+1) 
                    / PersonalPreference.DislikedWorkdays.Length)/8;
            }
            return 0;
        }


        void RecountShifts() {
            this.ShiftCount = 0;
            this.DislikedShiftCount = 0;
            for (int day = 0; day < Schedule.WEEKDAYS; day++) {
                bool isAssignedWork = AssignedWorkdays[day];
                if (isAssignedWork) {
                    ShiftCount++;
                    if (PersonalPreference.DislikedWorkdays.Contains(day)) DislikedShiftCount++;
                }
            }
        }

        private class Preferences {


            public int[] DislikedWorkdays { get; private set; }
            public int[] OffDays { get; private set; }
            Random Rand { get; set; }

            public Preferences() {
                this.Rand = new();
                RandomizeDisliked();
                RandomizeOffdays();

            }

            void RandomizeDisliked() {
                List<int> remainingDays = Enumerable.Range(0, Schedule.WEEKDAYS).ToList();
                int dislikedCount = 0;
                while (Rand.NextDouble() < DISLIKED_CHANCE && dislikedCount < MAX_DISLIKED_DAYS) {
                    dislikedCount++;
                }
                DislikedWorkdays = new int[dislikedCount];
                remainingDays.Add(6); //niedziele mają wyższą szansę
                
                for(int i=0; i<dislikedCount; i++) {
                    DislikedWorkdays[i] = remainingDays[Rand.Next(remainingDays.Count)];
                    remainingDays.RemoveAll(day => day == DislikedWorkdays[i]);
                }

            }

            void RandomizeOffdays() {
                int offdayCount = 0;
                List<int> remainingDays = Enumerable.Range(0, Schedule.WEEKDAYS).ToList();
                for (int i = 0; i < Schedule.WEEKDAYS; i++) {
                    if (Rand.NextDouble() < OFFDAY_CHANCE) {
                        offdayCount++;
                    }
                }
                remainingDays.AddRange(DislikedWorkdays); //zmienia tylko wagi nielubianych dni
              
                this.OffDays = new int[offdayCount];
                for(int i=0; i<offdayCount; i++) {
                    OffDays[i] = remainingDays[Rand.Next(remainingDays.Count)];
                    remainingDays.RemoveAll(day => day == OffDays[i]);
                }
            }
        }
    }
}
