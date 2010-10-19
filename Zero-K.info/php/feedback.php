<?php

	$user = $_REQUEST['user'] ;
	$email = $_REQUEST['email'] ;
	$type = $_REQUEST['type'] ;
	$comment = $_REQUEST['comment'] ;
	

require_once('recaptchalib.php');
$privatekey = "123456789123456789"; //needs replaced with real key
$resp = recaptcha_check_answer ($privatekey,
                                $_SERVER["REMOTE_ADDR"],
                                $_POST["recaptcha_challenge_field"],
                                $_POST["recaptcha_response_field"]);

if (!$resp->is_valid) {
  die ("The reCAPTCHA wasn't entered correctly. Go back and try it again." .
       "(reCAPTCHA said: " . $resp->error . ")");
}

	
/*do some stuff with it here*/
echo "$user says: $comment" ;
	
header( 'Location: index.php' ) ;

?>

