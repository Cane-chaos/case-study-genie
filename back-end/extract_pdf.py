import sys
try:
    import pypdf
except ImportError:
    try:
        import PyPDF2 as pypdf
    except ImportError:
        print("No pypdf or PyPDF2 found")
        sys.exit(1)

def extract_text(pdf_path, txt_path):
    try:
        reader = pypdf.PdfReader(pdf_path)
        with open(txt_path, 'w', encoding='utf-8') as f:
            for page in reader.pages:
                text = page.extract_text()
                if text:
                    f.write(text + "\n")
        print("Success")
    except Exception as e:
        print(f"Error: {e}")

if __name__ == "__main__":
    extract_text("tree-of-debate.pdf", ".tree2.txt")
