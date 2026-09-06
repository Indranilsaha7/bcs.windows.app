const { initializeApp } = require("firebase-admin/app");
const { getFirestore } = require("firebase-admin/firestore");
const path = require("path");

process.env.GOOGLE_APPLICATION_CREDENTIALS = path.join(__dirname, "../firebase-config.json");

initializeApp();
const db = getFirestore();

async function checkMailQueue() {
    try {
        console.log("Checking mail_queue...");
        const snapshot = await db.collection("mail_queue")
            .orderBy("createdAt", "desc")
            .limit(10)
            .get();
        
        if (snapshot.empty) {
            console.log("No documents in mail_queue.");
            return;
        }

        snapshot.forEach(doc => {
            const data = doc.data();
            console.log(`Doc ID: ${doc.id}`);
            console.log(`Status: ${data.status}`);
            console.log(`To Emails: ${JSON.stringify(data.to_emails)}`);
            console.log(`Error: ${data.error || 'None'}`);
            console.log(`CreatedAt: ${data.createdAt ? data.createdAt.toDate().toISOString() : 'N/A'}`);
            console.log("-----------------------");
        });
    } catch(err) {
        console.error(err);
    }
}
checkMailQueue();
