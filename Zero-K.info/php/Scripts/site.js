$(document).ready(function () {
	$('a.delete').click(function () {
		var answer = confirm('Really delete?');
		return answer // answer is a boolean
	});

	$(".rating").stars();
});

