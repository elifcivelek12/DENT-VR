using System.Collections.Generic;
using Firebase.Firestore; 

[FirestoreData]
public class SessionData
{
    [FirestoreProperty]
    public Timestamp SessionTimestamp { get; set; }

    [FirestoreProperty]
    public List<string> ConversationHistory { get; set; }

    [FirestoreProperty]
    public float PositiveScore { get; set; }

    [FirestoreProperty]
    public float NegativeScore { get; set; }

    [FirestoreProperty]
    public float NeutralScore { get; set; }

    [FirestoreProperty]
    public string Feedback { get; set; }
}