using System;
using System.Security.Cryptography;

namespace CrestronMastersMSS431InstructorProgram
{
    public class Password
    {

        public string ComputeHash(string Password)
        {
            var salt = new byte[] { 0x00, 0x41, 0x72, 0x08, 0x12, 0x22, 0x01, 0xff }; // 8 bytes hardcoded salt
            var Bytes = new Rfc2898DeriveBytes(Password, salt);
            return BitConverter.ToString(Bytes.GetBytes(24)).Replace("-", "");      // Return the first 24 bytes as String representation
        }

        private string GenerateSalt()                                               // this will generate a random salt as an example
                                                                                    // not used in this
        {
            var bytes = new byte[8];
            var rng = new RNGCryptoServiceProvider();
            rng.GetBytes(bytes);
            return Convert.ToString(bytes);
        }

        public bool CheckPassword(string Password, string PasswordToCheck)
        {
            if (Password.Equals(PasswordToCheck))                                    // Using string.Equals to do the work
            {
                return true;
            }
            else
            {
                return false;
            }

        }
        /*
         * The reason we use salts is to stop pre-computation attacks, such as rainbow tables. 
         * These attacks involve creating a database of hashes and their plain texts, so that hashes can be searched for and immediately
         * reversed into plain text.  Now, since the goal of the salt is only to prevent pre-generated databases from being created, 
         * it doesn't need to be encrypted or obscured in the database. You can store it in plain text. The goal is to force the attacker
         * to have to crack the hashes once he gets the database, instead of being able to just look them all up in a rainbow table.
         * If we had hundreds of passwords stored then random salts are important.  But this is an AV system with ONE password.
         * 
         * NOTE:  We can not generate a hash to compare the input password without the salt.  so we can add it to the hash,  giving the 
         * attacker the salt right there if they figure out if we stuffed it at the beginning or end. They need the code to do that.
         * This makes a random salt for each password kind of useless for us. I am instead going to use a hard coded one in my code.
         * I am including a random salt generator in the code for you to see how they are created. a hard coded salt in this case is actually more secure
         *  as randomly generated salts as they need the code to find it, instead of the salt being stored right there with the password hash.
         * 
         * I included a method to show you how to make a random salt, but it can not be used alone.  you will need to embed the salt at the beginning
         * or the end of the hash, and the compare has to be modified to strip the salt out and feed it to the hash function to compare it.
         * Using a fully random hash will make the code much more complex. If you need to store a ton of different passwords where random salts increase
         * the time to crack, you should have a good starting point with the code you see here.
         * 
         * If you are creating reusable code base and want the salt to be different per system, then generate a hash of the mac address
         * this will make it different from system to system,  you can then use the whole hash as the salt instead of only 8 like this example uses.
         * 
         * DISCLAIMER:   this code is for example and for learning only.  It's not complete, it's not production ready, it is your job as the programmer
         * to make the code complete and safe to use for your use case and customers environment.
         * this is for educational purposes only, no warranty or support for it is available. do not call true blue support asking for support on it.
         */



    }

}
