const { initializeApp } = require("firebase-admin/app");
const { getFirestore } = require("firebase-admin/firestore");
const path = require("path");

process.env.GOOGLE_APPLICATION_CREDENTIALS = path.join(__dirname, "../firebase-config.json");

initializeApp();
const db = getFirestore();

async function checkSettings() {
    try {
        const snapshot = await db.collection("settings").get();
        if (snapshot.empty) {
            console.log("No documents in settings.");
            return;
        }

        snapshot.forEach(doc => {
            console.log(`Doc ID: ${doc.id}`);
            console.log(doc.data());
            console.log("-----------------------");
        });
    } catch(err) {
        console.error(err);
    }
}
checkSettings();
