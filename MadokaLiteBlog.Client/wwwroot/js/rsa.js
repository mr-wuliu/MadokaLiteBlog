window.rsaFunctions = {
    encryptPassword: function (password, publicKey) {
        try {
            // 创建 RSA 公钥
            const publicKeyPem = `-----BEGIN PUBLIC KEY-----\n${publicKey}\n-----END PUBLIC KEY-----`;
            const forgePublicKey = forge.pki.publicKeyFromPem(publicKeyPem);
            
            // 使用 OAEP SHA256 加密
            const encrypted = forgePublicKey.encrypt(password, 'RSA-OAEP', {
                md: forge.md.sha256.create(),
                mgf1: {
                    md: forge.md.sha256.create()
                }
            });
            
            // 转换为 Base64
            return forge.util.encode64(encrypted);
        } catch (error) {
            console.error("加密失败:", error);
            throw error;
        }
    }
}; 