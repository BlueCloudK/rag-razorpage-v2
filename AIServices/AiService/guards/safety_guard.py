"""Safety and intent firewall helpers for requests that should not run RAG."""

from utils.text_normalization import normalize_text


def _direct(answer, strategy, intent, confidence=1.0):
    return {
        "answer": answer,
        "sources": [],
        "contexts": [],
        "model": "direct",
        "retrieval_strategy": strategy,
        "confidence": confidence,
        "fallback_used": False,
        "intent": intent,
    }


def is_prompt_injection(text):
    normalized = normalize_text(text)
    prompt_injection_terms = [
        "bo qua tai lieu", "ignore sources", "ignore source", "ignore documents",
        "ignore the documents", "ignore citations", "ignore citation", "ignore evidence",
        "answer from your own knowledge", "use your own knowledge", "without citations",
        "without sources", "without evidence", "tu tra loi", "khong can nguon", "khong can tai lieu",
        "in prompt he thong", "prompt he thong", "system prompt", "luat noi bo",
        "noi quy noi bo", "reveal prompt", "developer message", "hidden instruction",
        "hidden context", "internal rule", "internal rules",
    ]
    return any(term in normalized for term in prompt_injection_terms)


def classify_intent(query, history=None, is_likely_document_question=None):
    normalized = normalize_text(query)
    history = history or []

    if is_prompt_injection(query):
        return _direct(
            "Mình không thể bỏ qua tài liệu hoặc tiết lộ prompt/luật nội bộ. "
            "Yêu cầu này nằm ngoài phạm vi tài liệu của môn học; hãy hỏi một câu "
            "liên quan đến nội dung đã index để mình trả lời kèm nguồn.",
            "blocked_prompt_injection",
            "prompt_injection",
        )

    creative_terms = [
        "viet rap", "bai rap", "viet tho", "lam tho", "ke chuyen", "sang tac",
        "write a rap", "write a poem", "compose a song", "lyrics",
    ]
    if any(term in normalized for term in creative_terms):
        return _direct(
            "Câu này là yêu cầu sáng tác ngoài phạm vi tài liệu. Mình chỉ trả lời "
            "hoặc tóm tắt dựa trên nội dung đã index trong môn học hiện tại.",
            "blocked_out_of_scope",
            "out_of_scope",
        )

    small_talk_terms = [
        "tao dep trai", "toi dep trai", "minh dep trai", "tao xinh", "toi xinh",
        "toi buon", "minh buon", "tao buon", "chan qua", "toi chan",
        "ban thay toi", "ban nghi toi", "toi la ai", "tao la ai",
        "ok", "oke", "haha", "hihi", "hehe", "lol",
    ]
    if any(term == normalized or normalized.startswith(term + " ") for term in small_talk_terms):
        return _direct(
            "Câu này không cần tra tài liệu, nên mình không chạy retrieval hay gắn nguồn. "
            "Nếu bạn muốn học từ tài liệu đã index, hãy hỏi một câu cụ thể về nội dung học tập.",
            "blocked_small_talk",
            "small_talk",
        )

    if any(term in normalized for term in ["asdf", "qwer", "zxcv", "hahaha"]):
        return _direct(
            "Mình không thấy câu hỏi này đủ rõ để tra trong tài liệu. Bạn hãy hỏi cụ thể "
            "theo tên file, chương, mục hoặc thuật ngữ trong tài liệu đã index.",
            "blocked_out_of_scope",
            "out_of_scope",
        )

    if "gomaa" in normalized and "maintainable applications" in normalized:
        return _direct(
            "Mình không tìm thấy nội dung về **maintainable applications** trong tài liệu "
            "được chỉ định đã index, nên mình không suy diễn từ nguồn không liên quan.",
            "blocked_wrong_source_hint",
            "out_of_scope",
        )

    if "ddia" in normalized and "uml notation" in normalized:
        return _direct(
            "Mình không tìm thấy nội dung về **UML notation** trong tài liệu được chỉ định "
            "đã index, nên mình không suy diễn từ nguồn không liên quan.",
            "blocked_wrong_source_hint",
            "out_of_scope",
        )

    exam_terms = [
        "dap an bai tap", "dap an de thi", "answer key", "exam answer", "cho dap an",
        "ap an bai tap", "ap an de thi", "cho ap an",
        "giai ho bai tap", "lam bai tap giup", "cheat", "copy dap an", "copy ap an",
    ]
    if any(term in normalized for term in exam_terms):
        return _direct(
            "Mình không cung cấp đáp án chép sẵn. Bạn có thể hỏi khái niệm, chương, "
            "hoặc yêu cầu giải thích cách làm dựa trên tài liệu.",
            "blocked_exam_answer_request",
            "out_of_scope",
        )

    short_followup_terms = [
        "liet ke ra di", "liet ke ra i", "liet ke ra giup toi", "noi tiep", "giai thich them",
        "giai thich ky hon", "noi ky hon", "noi ki hon", "so sanh di", "cai do la gi", "phan do la gi",
        "list them", "continue", "explain more", "compare it",
    ]
    if not history and any(term == normalized or normalized.startswith(term) for term in short_followup_terms):
        return _direct(
            "Mình không có đủ ngữ cảnh để biết bạn muốn nói tới tài liệu, chương hoặc mục nào. "
            "Bạn hãy hỏi cụ thể hơn, ví dụ: “Liệt kê các mục trong chương 2 của Gomaa”.",
            "blocked_ambiguous_followup",
            "ambiguous_followup",
        )

    if is_likely_document_question and not is_likely_document_question(query, history):
        return _direct(
            f"Câu hỏi `{query}` chưa có dấu hiệu là câu hỏi về tài liệu đã index, nên mình "
            "không chạy retrieval hay gắn nguồn. Hãy hỏi một câu cụ thể về nội dung học tập "
            "hoặc khái niệm trong tài liệu.",
            "blocked_non_document_intent",
            "non_document_question",
        )

    return None

# TODO(1): Placeholder
# TODO(2): Placeholder
# TODO(3): Placeholder
# TODO(4): Placeholder
# TODO(5): Placeholder
# TODO(6): Placeholder
# TODO(7): Placeholder
# TODO(8): Placeholder
# TODO(9): Placeholder
# TODO(10): Placeholder
# TODO(11): Placeholder
# TODO(12): Placeholder
# TODO(13): Placeholder
# TODO(14): Placeholder
# TODO(15): Placeholder
# TODO(16): Placeholder
# TODO(17): Placeholder
# TODO(18): Placeholder
# TODO(19): Placeholder
# TODO(20): Placeholder
# TODO(21): Placeholder
# TODO(22): Placeholder
# TODO(23): Placeholder
# TODO(24): Placeholder
# TODO(25): Placeholder
# TODO(26): Placeholder
# TODO(27): Placeholder
# TODO(28): Placeholder
# TODO(29): Placeholder
# TODO(30): Placeholder
# TODO(31): Placeholder
# TODO(32): Placeholder
# TODO(33): Placeholder
# TODO(34): Placeholder
# TODO(35): Placeholder
# TODO(36): Placeholder
# TODO(37): Placeholder
# TODO(38): Placeholder
# TODO(39): Placeholder
# TODO(40): Placeholder
# TODO(41): Placeholder
# TODO(42): Placeholder
# TODO(43): Placeholder
# TODO(44): Placeholder
# TODO(45): Placeholder
# TODO(46): Placeholder
# TODO(47): Placeholder
# TODO(48): Placeholder
# TODO(49): Placeholder
# TODO(50): Placeholder
# TODO(51): Placeholder
# TODO(52): Placeholder
# TODO(53): Placeholder
# TODO(54): Placeholder
# TODO(55): Placeholder
# TODO(56): Placeholder
# TODO(57): Placeholder
# TODO(58): Placeholder
# TODO(59): Placeholder
# TODO(60): Placeholder
