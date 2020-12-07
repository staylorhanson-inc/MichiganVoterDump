using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Runtime.Serialization;
using System.IO;


namespace MichiganVoterDope
{
    public class Program
    {
        public FileStream fs;
        public static string JsonAbsenteeVotingBallotDataDump
        {
            get
            {
                return _jsonAbsenteeVotingBallotDump;
            }

            set
            {
                _jsonAbsenteeVotingBallotDump = value;
            }
        }

        public static VoteShares vs;
        public static Timesery ts;
        public static Root root;
        public static List<PotentialBallotDumpAnamoly> listOfAnamolies;

        private static string _jsonAbsenteeVotingBallotDump;

        static void Main(string[] args)
        {
            GetJsonStringFromFile();
            DeserializeJsonString();
            RecordVotingAnomalies();
            GatherStatisticsAboutTheAnamolies();
        }

        public static void GetJsonStringFromFile()
        {
            var fileName = @"C:\Users\taylo02167\Documents\voterdumpDataMichigan.json";

            using (FileStream fs = File.OpenRead(fileName))
            {
                byte[] buf = new byte[1024];
                int c;

                while ((c = fs.Read(buf, 0, buf.Length)) > 0)
                {
                    _jsonAbsenteeVotingBallotDump += (Encoding.UTF8.GetString(buf, 0, c));
                }
            }
        }

        public static void DeserializeJsonString()
        {
            root = JsonConvert.DeserializeObject<Root>(_jsonAbsenteeVotingBallotDump);
        }
        
        public static void RecordVotingAnomalies()
        {
            int ballotDumpBatchNumber = 0;
            listOfAnamolies = new List<PotentialBallotDumpAnamoly>();
            PotentialBallotDumpAnamoly anamoly = new PotentialBallotDumpAnamoly();
            foreach (Timesery ts in root.timeseries)
            {

                if (ballotDumpBatchNumber == 0)
                {
                    anamoly.ballotBatchNumber = ballotDumpBatchNumber;
                    anamoly.ballotProcessTime = ts.timestamp;
                    anamoly.bidenCurrentVoteTotal = (Int32)ts.vote_shares.bidenj * ts.votes;
                    anamoly.trumpCurrentVoteTotal = (Int32)ts.vote_shares.trumpd * ts.votes;
                    anamoly.IsBidenAnamoly = false;
                    anamoly.IsTrumpAnamoly = false;
                    listOfAnamolies.Add(anamoly);
                    ballotDumpBatchNumber++;
                    continue;
                }
                
                 anamoly = new PotentialBallotDumpAnamoly();
                 anamoly.ballotBatchNumber = ballotDumpBatchNumber;
                 anamoly.ballotProcessTime = ts.timestamp;
                 anamoly.bidenPreviousVoteTotal = listOfAnamolies[ballotDumpBatchNumber - 1].bidenCurrentVoteTotal;
                 anamoly.trumpPreviousVoteTotal = listOfAnamolies[ballotDumpBatchNumber - 1].trumpCurrentVoteTotal;
                 anamoly.bidenCurrentVoteTotal = ts.vote_shares.bidenj * ts.votes;
                 anamoly.trumpCurrentVoteTotal = ts.vote_shares.trumpd * ts.votes;

                 if ((anamoly.bidenCurrentVoteTotal < anamoly.bidenPreviousVoteTotal))
                 {
                     anamoly.IsBidenAnamoly = true;
                 }
                 if (anamoly.trumpCurrentVoteTotal < anamoly.trumpPreviousVoteTotal)
                 {
                      anamoly.IsTrumpAnamoly = true;
                 }
                

                listOfAnamolies.Add(anamoly);
                ballotDumpBatchNumber++;
                
            }

        }

        public static void GatherStatisticsAboutTheAnamolies()
        {
            double totalTrumpLoss = 0;
            double totalBidenLoss = 0;
            double numberOfBatches = 0;
            TrumpVotingAnamolies tva = new TrumpVotingAnamolies();
            BidenVotingAnamolies bva = new BidenVotingAnamolies();

            foreach (PotentialBallotDumpAnamoly anamoly in listOfAnamolies)
            {
                if(anamoly.IsBidenAnamoly)
                {
                    bva.TotalVotesLost += (anamoly.bidenPreviousVoteTotal - anamoly.bidenCurrentVoteTotal);
                    bva.TotalNumber += 1;
                }
                else if(anamoly.IsTrumpAnamoly)
                {
                    tva.TotalVotesLost += (anamoly.trumpPreviousVoteTotal - anamoly.trumpCurrentVoteTotal);
                    tva.TotalNumber += 1;
                }
                numberOfBatches += 1;
            }

            tva.PercentGlitches = (tva.TotalNumber / numberOfBatches) * 100.0;
            bva.PercentGlitches = (bva.TotalNumber / numberOfBatches) * 100.0;
            OutputStatisticsTOConsole(tva, bva);
        }

        public static void OutputStatisticsTOConsole(TrumpVotingAnamolies ta, BidenVotingAnamolies ba)
        {
            Console.WriteLine(String.Format("Trump total Votes Lost: {0}.", ta.TotalVotesLost));
            Console.WriteLine(String.Format("Trump percent glitches: {0}", ta.PercentGlitches));
            Console.WriteLine(String.Format("Number of Glitches for Trump: {0}", ta.TotalNumber));
            Console.WriteLine("Hit the Enter Key to See Biden's Statistics");
            Console.ReadLine();
            Console.WriteLine(String.Format("Biden total Votes Lost: {0}.", ba.TotalVotesLost));
            Console.WriteLine(String.Format("Biden percent glitches: {0}", ba.PercentGlitches));
            Console.WriteLine(String.Format("Number of Glitches for Biden: {0}", ba.TotalNumber));
            Console.ReadLine();
        }

    }

    public class VoteShares
    {
        public double trumpd { get; set; }
        public double bidenj { get; set; }
    }


    public class Timesery
    {
        public VoteShares vote_shares { get; set; }
        public int votes { get; set; }
        public int eevp { get; set; }
        public string eevp_source { get; set; }
        public DateTime timestamp { get; set; }
    }

    public class Root
    {
        public List<Timesery> timeseries { get; set; }

    }

    public class PotentialBallotDumpAnamoly
    {
        public int ballotBatchNumber;
        public DateTime ballotProcessTime;
        public Double trumpPreviousVoteTotal;
        public Double bidenPreviousVoteTotal;
        public Double trumpCurrentVoteTotal;
        public Double bidenCurrentVoteTotal;
        public bool IsBidenAnamoly;
        public bool IsTrumpAnamoly;
    }

public abstract class VotingAnamoly
{
    public Double TotalNumber;
    public Double PercentGlitches;
    public Double TotalVotesLost;
}
public class TrumpVotingAnamolies : VotingAnamoly { }


public class BidenVotingAnamolies : VotingAnamoly { }

}
